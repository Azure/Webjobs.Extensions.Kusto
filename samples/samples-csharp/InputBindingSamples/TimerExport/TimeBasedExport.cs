// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.InputBindingSamples.TimerExportSample
{
    // A timer runs every 5 seconds and then exports the data in that duration to a RMQ
    // To make it harder, we will use a dynamic predicate as well
    public static class TimeBasedExport
    {
        [FunctionName("TimeBasedExport")]
        public static async Task Run(
            [TimerTrigger("*/5 * * * * *")] TimerInfo exportTimer,
            IBinder binder, ILogger log,
            [RabbitMQ(QueueName = "bindings.test.queue", ConnectionStringSetting = "rabbitMQConnectionAppSetting")] IAsyncCollector<Product> outputProducts)
        {
            DateTime? dateOfRun = exportTimer?.ScheduleStatus?.Last;
            DateTime runTime = dateOfRun == null ? DateTime.UtcNow : exportTimer.ScheduleStatus.Last.ToUniversalTime();
            string startTime = runTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            // Runs every one min, so query this every one min
            string endTime = runTime.AddSeconds(5).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            var kustoAttribute = new KustoAttribute(SampleConstants.DatabaseName)
            {
                Connection = "KustoConnectionString",
                KqlCommand = "declare query_parameters (name:string,startTime:string,endTime:string);Products | extend ig=ingestion_time() | where Name has name | where ig >= todatetime(startTime) and ig <= todatetime(endTime) | order by ig asc",
                KqlParameters = $"@name=Item,@startTime={startTime},@endTime={endTime}"
            };
            // List of ingested records
            var exportedRecords = (await binder.BindAsync<IEnumerable<Product>>(kustoAttribute)).ToList();
            // Count for logs
            log.LogInformation($"Querying data between {startTime} and {endTime} yielded {exportedRecords.Count} records");
            // Send them to a continuous export topic. Just transform the names in this case
            foreach (Product item in exportedRecords)
            {
                item.Name = $"R-MQ-{item.ProductID}";
                await outputProducts.AddAsync(item);
            }
        }
    }
}