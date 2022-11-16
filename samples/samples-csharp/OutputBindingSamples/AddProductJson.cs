// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    internal class AddProductJson
    {
        [FunctionName("AddProductJson")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproductjson")]
            HttpRequest req, ILogger log,
            [Kusto(database:"sdktestsdb" ,
            tableName:"Products" ,
            Connection = "KustoConnectionString")] out JObject json)
        {
            var product = new Product
            {
                Name = req.Query["name"],
                ProductID = int.Parse(req.Query["productId"]),
                Cost = int.Parse(req.Query["cost"])
            };
            string productString = string.Format(CultureInfo.InvariantCulture, "(Name:{0} ID:{1} Cost:{2})", product.Name, product.ProductID, product.Cost);
            json = JObject.FromObject(product);
            log.LogInformation("Ingested product JSON {}", productString);
        }
    }
}
