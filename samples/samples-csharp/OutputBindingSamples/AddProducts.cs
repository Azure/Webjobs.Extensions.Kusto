// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddProducts
    {
        [FunctionName("AddProducts")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproducts")]
            HttpRequest req, ILogger log,
            [Kusto(database:"sdktestsdb" ,
            tableName:"Products" ,
            Connection = "KustoConnectionString")] IAsyncCollector<Product> collector)
        {
            log.LogInformation($"AddProduct function started");
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                collector.AddAsync(new Product()
                {
                    Name = req.Query["name"] + random.Next(1000),
                    ProductID = int.Parse(req.Query["productId"]) + random.Next(1000),
                    Cost = int.Parse(req.Query["cost"]) + random.Next(19990)
                });
            }
        }
    }
}
