// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.BlobTriggerIngestSample
{
    public class IngestBlobToKusto
    {
        [FunctionName("IngestBlobToKusto")]
        public static async Task Run(
            [BlobTrigger("samples-blob-ingest/*.csv.gz", Connection = "StorageConnectionString")] BlobClient blobClient, IBinder binder, ILogger logger)
        {
            BlobProperties blobProperties1 = await blobClient.GetPropertiesAsync();
            logger.LogInformation("Blob sample-container/sample-blob-1 has been updated on: {datetime}", blobProperties1.LastModified);
            // Create a SAS token that's valid for one hour.
            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            // Specify read permissions for the SAS.
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Use the key to get the SAS token.
            Uri sasToken = blobClient.GenerateSasUri(sasBuilder);
            logger.LogTrace("SAS token for blob sample-container/sample-blob-1  is: {sasToken}", sasToken);
            var kustoIngest = new KustoAttribute("e2e")
            {
                Connection = "KustoConnectionString",
                KqlCommand = $".ingest into table eshopclothing ('{sasToken}')"
            };
            JArray ingestResult = await binder.BindAsync<JArray>(kustoIngest);
            logger.LogInformation("Ingestion result {ingestResult}", ingestResult);
        }
    }
}