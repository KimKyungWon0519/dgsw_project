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
using System.Net.WebSockets;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Linq;
using System.Xml.Linq;

namespace FunctionApp2
{
    public static class Function2
    {
        private const string StorageConnetinoStringKey = "AzureWebJobsStorage";

        [FunctionName("IdentifyFaceHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "faces/identify")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //Setup enviroment variables
            var sasToken = Environment.GetEnvironmentVariable("Blob__SasToken");
            var containerName = Environment.GetEnvironmentVariable("Blob__Container");
            var personGroup = Environment.GetEnvironmentVariable("Blob__PersonGroup");
            var numberOfPhotos = Convert.ToInt32(Environment.GetEnvironmentVariable("Blob__NumberOfPhotos"));

            var tableName = Environment.GetEnvironmentVariable("Table__Name");

            var authKey = Environment.GetEnvironmentVariable("Face__AuthKey");
            var endpoint = Environment.GetEnvironmentVariable("Face__Endpoint");
            var confidence = Convert.ToDouble(Environment.GetEnvironmentVariable("Face__Confidence"));

            //Setup dependencies
            var connetionString = Environment.GetEnvironmentVariable(StorageConnetinoStringKey);
            var storageAccount = CloudStorageAccount.Parse(connetionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var tableClient = storageAccount.CreateCloudTableClient();

            //Receive face image
            var payload = await new StreamReader(req.Body).ReadToEndAsync().ConfigureAwait(false);
            var segments = payload.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var encoded = segments[1];
            var contentType = segments[0].Split(new[] { ":", ";" }, StringSplitOptions.RemoveEmptyEntries)[1];

            //Get face images from blob storage
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);

            var segment = await container.ListBlobsSegmentedAsync($"{personGroup}/", true, BlobListingDetails.Metadata, numberOfPhotos, null, null, null)
                                         .ConfigureAwait(false);

            var faces = segment.Results.Select(p => (CloudBlockBlob)p).ToList();

            //Upload face received
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);
            var uploaded = container.GetBlockBlobReference($"{personGroup}/{Guid.NewGuid().ToString()}.png");
            uploaded.Properties.ContentType = contentType;

            var bytes = Convert.FromBase64String(encoded);

            await uploaded.UploadFromByteArrayAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

            //Check number of face image from blob storage
            if (faces.Count < numberOfPhotos)
            {
                return new CreatedResult(uploaded.Uri, $"Need {numberOfPhotos - faces.Count} more photo(s).");
            }

            //Add training history record to table storage
            var personGroupId = Guid.NewGuid().ToString();

            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync().ConfigureAwait(false);

            var entity = new FaceEntity(personGroup, personGroupId);
            var operation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(operation).ConfigureAwait(false);

            //Train face images as control group

            //Identify face

            //Comfirm face




            var result = new OkResult();

            return result;
        }
    }
}
