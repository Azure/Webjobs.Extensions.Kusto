// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    public static class AddProduct
    {
        [FunctionName("AddProductUni")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductuni")]
            HttpRequest req, ILogger log,
            [Kusto(Database:"sdktestsdb" ,
            TableName ="Products" ,
            Connection = "KustoConnectionString")] out Product product)
        {
            log.LogInformation($"AddProduct function started");
            string body = new StreamReader(req.Body).ReadToEnd();
            product = JsonConvert.DeserializeObject<Product>(body);
            string productString = string.Format(CultureInfo.InvariantCulture, "(Name:{0} ID:{1} Cost:{2})",
                        product.Name, product.ProductID, product.Cost);
            log.LogInformation("Ingested product {}", productString);
        }
    }
}
