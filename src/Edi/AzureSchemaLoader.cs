using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace Integrations.EDI
{
    public class AzureSchemaLoader : ISchemaLoader
    {
        private readonly string storageAccountName;
        private readonly string containerName;
        private readonly BlobContainerClient containerClient;
        private readonly Dictionary<string, TransactionSchema> cache = new Dictionary<string, TransactionSchema>();

        public AzureSchemaLoader(string storageAccountName, string containerName = "schemas")
        {
            this.storageAccountName = storageAccountName ?? throw new ArgumentNullException(nameof(storageAccountName));
            this.containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));

            // Create blob service client using DefaultAzureCredential
            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{storageAccountName}.blob.core.windows.net"),
                new DefaultAzureCredential()
            );

            this.containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }

        public TransactionSchema? LoadSchema(string release, string transactionSetId)
        {
            var cacheKey = $"{release}_{transactionSetId}";
            if (cache.TryGetValue(cacheKey, out var cached))
                return cached;

            try
            {
                // Blob path: [Release]/[TransactionSet].json
                var blobName = $"{release}/{transactionSetId}.json";
                var blobClient = containerClient.GetBlobClient(blobName);

                // Download blob content synchronously
                var response = blobClient.Download();
                using var reader = new StreamReader(response.Value.Content);
                var json = reader.ReadToEnd();

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var x12Schema = JsonSerializer.Deserialize<X12Schema>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (x12Schema != null)
                    {
                        // Convert X12Schema to TransactionSchema (same logic as LocalSchemaLoader)
                        var schema = ConvertToTransactionSchema(x12Schema);
                        cache[cacheKey] = schema;
                        return schema;
                    }
                }
            }
            catch
            {
                // Blob not found or authentication failure - return null
            }

            return null;
        }

        private static TransactionSchema ConvertToTransactionSchema(X12Schema x12Schema)
        {
            var schema = new TransactionSchema
            {
                TransactionSetId = x12Schema.TransactionSet.Id,
                Release = x12Schema.TransactionSet.Version
            };

            // Extract ST segment elements from header area
            var stSegment = System.Linq.Enumerable.FirstOrDefault(x12Schema.HeaderArea, s => s.SegmentId == "ST");
            if (stSegment != null)
            {
                int pos = 1;
                foreach (var el in stSegment.Elements)
                {
                    schema.Elements.Add(new ElementSchema
                    {
                        Position = pos++,
                        Name = el.Id,
                        Required = el.Required == "M",
                        Type = el.Type == "ID" || el.Type.StartsWith("N") ? "N" : "AN",
                        MaxUse = 1
                    });
                }
            }

            // Extract loops from detail area
            foreach (var loop in x12Schema.DetailArea)
            {
                var anchorSegment = System.Linq.Enumerable.FirstOrDefault(loop.Segments);
                if (anchorSegment != null)
                {
                    schema.Loops.Add(new LoopSchema
                    {
                        Name = loop.LoopId,
                        Anchor = anchorSegment.SegmentId,
                        MaxUse = loop.MaxUse
                    });
                }
            }

            return schema;
        }
    }
}
