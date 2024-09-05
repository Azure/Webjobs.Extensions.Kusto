// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Web;
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
            [BlobTrigger("samples-blob-ingest/{name}", Connection = "StorageConnectionString")] BlobClient blobClient, string name, IBinder binder, ILogger logger)
        {
            BlobProperties blobProperties1 = await blobClient.GetPropertiesAsync();
            logger.LogInformation("Blob sample-container/{name} has been updated on: {datetime}", name, blobProperties1.LastModified);
            // Create a SAS token that's valid for one hour.
            Uri sasToken = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddHours(1));
            string blobUploadUri = HttpUtility.UrlDecode(sasToken.OriginalString);
            logger.LogTrace("SAS token for blob sample-container  is: {sasToken}", blobUploadUri);
            string ingestCommand = $".ingest into table eshopclothing ('{blobUploadUri}')";
            var kustoIngest = new KustoAttribute("e2e")
            {
                Connection = "KustoConnectionString",
                KqlCommand = ingestCommand
            };
            logger.LogInformation("Ingest command is: {ingestCommand}", ingestCommand);
            JArray ingestResult = await binder.BindAsync<JArray>(kustoIngest);
            logger.LogInformation("Ingestion result {ingestResult}", ingestResult);
        }
    }
}