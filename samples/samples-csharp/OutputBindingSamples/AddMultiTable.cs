// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using System.IO;
using Kusto.Cloud.Platform.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddMultiTable
    {
        [FunctionName("AddMultiTable")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addmulti")]
            HttpRequest req, ILogger log,
            [Kusto(Database:SampleConstants.DatabaseName ,
            TableName =SampleConstants.ProductsTable ,
            Connection = "KustoConnectionString")] IAsyncCollector<Product> productsCollector,
                        [Kusto(Database:SampleConstants.DatabaseName ,
            TableName =SampleConstants.ProductsChangeLogTable ,
            Connection = "KustoConnectionString")] IAsyncCollector<ProductsChangeLog> changeCollector)
        {
            log.LogInformation($"AddProducts function started");
            string body = new StreamReader(req.Body).ReadToEnd();
            Product[] products = JsonConvert.DeserializeObject<Product[]>(body);
            products.ForEach(p =>
            {
                productsCollector.AddAsync(p);
                changeCollector.AddAsync(new ProductsChangeLog { CreatedAt = DateTime.UtcNow, ProductID = p.ProductID });
            });
            productsCollector.FlushAsync();
            changeCollector.FlushAsync();
            return products != null ? new ObjectResult(products) { StatusCode = StatusCodes.Status201Created } : new BadRequestObjectResult("Please pass a well formed JSON Product array in the body");
        }
    }
}
