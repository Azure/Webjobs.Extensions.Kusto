// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Kusto;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.InputBindingSamples.AdminCommands
{
    public static class IngestStormsData
    {
        [FunctionName("IngestStormsData")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ingeststorms")]
#pragma warning disable IDE0060 // Remove unused parameter
            HttpRequest req,
#pragma warning restore IDE0060 // Remove unused parameter
            [Kusto(Database:"sdktestsdb" ,
            KqlCommand = ".ingest into table Storms ('https://kustosamples.blob.core.windows.net/samplefiles/StormEvents.csv') with (ignoreFirstRecord = true, format = \"csv\")" ,
            Connection = "KustoConnectionString")]
            IAsyncEnumerable<IngestResults> ingestResults)
        {

            return new OkObjectResult(ingestResults);
        }
    }

    public class IngestResults
    {
        public string ExtentId { get; set; }
        public string ItemLoaded { get; set; }
        public string OperationId { get; set; }
        public bool HasErrors { get; set; }

    }
}