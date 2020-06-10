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

namespace FunctionApp2
{
    public static class Function2
    {
        private const string StorageConnetinoStringKey = "AzureWebJobsStorage";

        [FunctionName("Function2")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "faces/register")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var payload = await new StreamReader(req.Body)
                .ReadToEndAsync().ConfigureAwait(false);
            var segments = payload.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var encoded = segments[1];
            var contentType = segments[0].Split(new[] { ":", ";" }, StringSplitOptions.RemoveEmptyEntries)[1];

            var connetionString = Environment.GetEnvironmentVariable(StorageConnetinoStringKey);
            var client = CloudStorageAccount.Parse(connetionString).CreateCloudBlobClient();
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
