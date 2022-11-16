// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddProduct
    {
        [FunctionName("AddProduct")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproduct")]
            HttpRequest req, ILogger log,
            [Kusto(database:"sdktestsdb" ,
            tableName:"Products" ,
            Connection = "KustoConnectionString")] out Product product)
        {
            log.LogInformation($"AddProduct function started");
            product = new Product
            {
                Name = req.Query["name"],
                ProductID = int.Parse(req.Query["productId"]),
                Cost = int.Parse(req.Query["cost"])
            };
            string productString = string.Format(CultureInfo.InvariantCulture, "(Name:{0} ID:{1} Cost:{2})",
                        product.Name, product.ProductID, product.Cost);
            log.LogInformation("Ingested product {}", productString);
        }
    }
}
