// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.IO;
using Microsoft.AspNetCore.Http;
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
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproducts")]
            HttpRequest req, ILogger log,
            [Kusto(Database:"sdktestsdb" ,
            TableName ="Products" ,
            Connection = "KustoConnectionString")] IAsyncCollector<Product> collector)
        {
            log.LogInformation($"AddProducts function started");
            string body = new StreamReader(req.Body).ReadToEnd();
            ProductList products = JsonConvert.DeserializeObject<ProductList>(body);
            products.Products.ForEach(p =>
            {
                collector.AddAsync(p);
            });
        }
    }
}
