// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddProductsWithMapping
    {
        [FunctionName("AddProductsWithMapping")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductswithmapping")]
            HttpRequest req, ILogger log,
            [Kusto(Database:SampleConstants.DatabaseName ,
            TableName =SampleConstants.ProductsTable ,
            MappingRef = "item_to_product_json",
            Connection = "KustoConnectionString")] out Item item)
        {
            log.LogInformation($"AddProductsWithMapping function started");
            string body = new StreamReader(req.Body).ReadToEnd();
            item = JsonConvert.DeserializeObject<Item>(body);
            string productString = string.Format(CultureInfo.InvariantCulture, "(ItemName:{0} ItemID:{1} ItemCost:{2})",
                        item.ItemName, item.ItemID, item.ItemCost);
            log.LogInformation("Ingested item {}", productString);
            return item != null ? new ObjectResult(item) { StatusCode = StatusCodes.Status201Created } : new BadRequestObjectResult("Please pass a well formed JSON Product array in the body");
        }
    }
}
