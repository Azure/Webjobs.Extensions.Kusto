// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.InputBindingSamples
{
    public static class GetProductsMulti
    {
        [Function("GetProductsMulti")]
        public static Task<List<Product>> Run(
#pragma warning disable 
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproductsmq/{productId}/{name}")] HttpRequestData req,
#pragma warning disable
            [KustoInput(Database: "sdktestsdb",
            KqlCommand = "declare query_parameters (productId:long);Products | where ProductID == productId",
            KqlParameters = "@productId={productId}",Connection = "KustoConnectionString")] List<Product> productsQuery,
            [KustoInput(Database: "sdktestsdb",
            KqlCommand = "declare query_parameters (name:string);GetProductsByName(name)",
            KqlParameters = "@name={name}",Connection = "KustoConnectionString")] List<Product> productsFunction)
        {
            IEnumerable<Product> products = productsQuery.Concat(productsFunction);
            return Task.FromResult(products.ToList());
        }
    }
}