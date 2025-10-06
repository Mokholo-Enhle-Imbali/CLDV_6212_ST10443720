using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;
using System.Web;

namespace ABCRetailersFunction.Functions
{
    public class BlobFunctions
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobFunctions(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        [Function("OnProductImageUploaded")]
        public async Task OnProductImageUploaded(
      [QueueTrigger("image-processing")] string blobName,
      FunctionContext ctx)
        {
            var log = ctx.GetLogger("OnProductImageUploaded");

            // TEMPORARY: Throw exception to keep message in queue for screenshot
            log.LogInformation($"🖼️ IMAGE QUEUE MESSAGE CAPTURED: {blobName}");
            throw new Exception("TEMPORARY: Keeping message in queue for screenshot");

            /*
            // NORMAL CODE (commented out):
            var containerClient = _blobServiceClient.GetBlobContainerClient("product-images");
            var blobClient = containerClient.GetBlobClient(blobName);

            if (await blobClient.ExistsAsync())
            {
                var downloadResult = await blobClient.DownloadContentAsync();
                var imageData = downloadResult.Value.Content.ToArray();
                log.LogInformation($"Product image uploaded: {blobName}, size={imageData.Length} bytes");
            }
            */
        }
    }
}