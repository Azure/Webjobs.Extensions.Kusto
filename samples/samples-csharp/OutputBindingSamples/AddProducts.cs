// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


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
    public static class AddProducts
    {
        [FunctionName("AddProducts")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproduct")]
            HttpRequest req, ILogger log,
            [Kusto(Database:SampleConstants.DatabaseName ,
            TableName =SampleConstants.ProductsTable ,
            Connection = "KustoConnectionString")] IAsyncCollector<Product> collector)
        {
            log.LogInformation($"AddProducts function started");
            string body = new StreamReader(req.Body).ReadToEnd();
            Product[] products = JsonConvert.DeserializeObject<Product[]>(body);
            products.ForEach(p =>
            {
                collector.AddAsync(p);
            });
            collector.FlushAsync();
            return products != null ? new ObjectResult(products) { StatusCode = StatusCodes.Status201Created } : new BadRequestObjectResult("Please pass a well formed JSON Product array in the body");
        }
    }
}