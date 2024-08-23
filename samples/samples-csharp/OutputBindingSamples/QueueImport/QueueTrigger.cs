// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples.QueueImport
{
    public class QueueTrigger
    {
        [Disable("QueueFunctionDisabled")]
        [FunctionName("QueueTriggerBinding")]
        [return: Kusto(Database: SampleConstants.DatabaseName,
                    TableName = SampleConstants.ProductsTable,
                    Connection = "KustoConnectionString")]
        public static Product Run(
            [RabbitMQTrigger(queueName: "bindings.test.queue", ConnectionStringSetting = "rabbitMQConnectionAppSetting")] Product product,
            ILogger log)
        {
            log.LogInformation($"Dequeued product {product.ProductID}");
            return product;
        }
    }
}