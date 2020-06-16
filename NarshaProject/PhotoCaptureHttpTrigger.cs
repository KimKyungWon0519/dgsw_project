using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using Microsoft.WindowsAzure.Storage;

namespace NarshaProject
{
    public static class PhotoCaptureHttpTrigger
    {
        private const string StorageConnectionStringKey = "AzureWebJobsStorage";

        [FunctionName("PhotoCaptureHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "faces/register")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var payload = await new StreamReader(req.Body).ReadToEndAsync().ConfigureAwait(false);
            var segments = payload.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var encoded = segments[1];
            var contentType = segments[1].Split(new[] { ":", ";" }, StringSplitOptions.RemoveEmptyEntries)[1];
            var connectionString = Environment.GetEnvironmentVariable(StorageConnectionStringKey);
            var client = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient();
            var container = client.GetContainerReference("faces");

            await container.CreateIfNotExistsAsync().ConfigureAwait(false);
            
            var blob = container.GetBlockBlobReference(Guid.NewGuid().ToString());
            
            blob.Properties.ContentType = contentType;

            var bytes = Convert.FromBase64String(encoded);

            await blob.UploadFromByteArrayAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

            var result = new CreatedResult(blob.Uri, null);

            return result;
        }
    }
}
