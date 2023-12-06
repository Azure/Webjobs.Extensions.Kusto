// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.EventHub.Kusto.Samples.Common;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;


namespace Microsoft.Azure.WebJobs.Extensions.EventHub.Kusto.Samples
{
    public static class EventHubToKusto
    {
        [FunctionName("EventHubToKusto")]
        [return: KustoOutput(Database: SampleConstants.DatabaseName,
                    TableName = SampleConstants.ProductsTable,
                    Connection = "KustoConnectionString")]
        public static async Task Run([EventHubTrigger("samples-workitems", Connection = "")] Product[] products, ILogger log)
        {
            log.LogInformation($"C# Event Hub trigger function processed a message: {products.Length}");
            return products
        }
    }
}
