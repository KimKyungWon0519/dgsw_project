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

namespace NarshaProject
{
    public static class RenderPageHttpTrigger
    {
        [FunctionName("RenderPageHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "pages/render")] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var filePath = $"{context.FunctionAppDirectory}/photo-capture.html";
            var file = await File.ReadAllTextAsync(filePath, Encoding.UTF8).ConfigureAwait(false);
            var result = new ContentResult()
            {
                Content = file,
                StatusCode = 200,
                ContentType = "text/html"
            };

            return result;
        }
    }
}
