using ABCRetailersFunction.Helpers;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABCRetailersFunction.Functions
{
    public class UploadFunctions
    {
        private readonly string _conn;
        private readonly string _proofs;

        public UploadFunctions(IConfiguration cfg)
        {
            _conn = cfg["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("STORAGE_CONNECTION missing");
            _proofs = cfg["BLOB_PAYMENT_PROOFS"] ?? "payment-proofs";
        }

        [Function("Uploads_ProofOfPayment")]
        public async Task<HttpResponseData> Proof(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "uploads/proof-of-payment")] HttpRequestData req)
        {
            try
            {
                // Check content type
                var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
                if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                    return await CreateErrorResponse(req, "Expected multipart/form-data", HttpStatusCode.BadRequest);

                // Parse multipart form
                var form = await MultipartHelper.ParseAsync(req.Body, contentType);
                var file = form.Files.FirstOrDefault(f => f.FieldName == "ProofOfPayment");
                if (file is null || file.Data.Length == 0)
                    return await CreateErrorResponse(req, "ProofOfPayment file is required", HttpStatusCode.BadRequest);

                var orderId = form.Text.GetValueOrDefault("OrderId") ?? "Unknown";
                var customerName = form.Text.GetValueOrDefault("CustomerName") ?? "Unknown";

                // Upload to Blob Storage only (skip file share for now)
                var container = new BlobContainerClient(_conn, _proofs);
                await container.CreateIfNotExistsAsync();

                var blobName = $"{Guid.NewGuid():N}-{file.FileName}";
                var blob = container.GetBlobClient(blobName);

                file.Data.Position = 0;
                await blob.UploadAsync(file.Data, overwrite: true);

                // Return success
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new
                {
                    fileName = blobName,
                    blobUrl = blob.Uri.ToString(),
                    originalFileName = file.FileName,
                    fileSize = file.Data.Length,
                    uploadedAt = DateTimeOffset.UtcNow
                });
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                return response;
            }
            catch (Exception ex)
            {
                return await CreateErrorResponse(req, $"Upload failed: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, string message, HttpStatusCode statusCode)
        {
            var response = req.CreateResponse(statusCode);
            var errorResult = new { error = message };
            await response.WriteAsJsonAsync(errorResult);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            return response;
        }
    }
}