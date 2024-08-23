// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.InputBindingSamples
{
    public static class GetProductsFunctionAsyncEnumerable
    {
        [FunctionName("GetProductsFunctionAsyncEnumerable")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproductsfunctions-ae/{name}")]
#pragma warning disable IDE0060 // Remove unused parameter
            HttpRequest req,
#pragma warning restore IDE0060 // Remove unused parameter
            [Kusto(Database:SampleConstants.DatabaseName ,
            KqlCommand = "declare query_parameters (name:string);GetProductsByName(name)" ,
            KqlParameters = "@name={name}",
            Connection = "KustoConnectionString")]
            IAsyncEnumerable<Product> products)
        {
            IAsyncEnumerator<Product> enumerator = products.GetAsyncEnumerator();
            var productList = new List<Product>();
            while (await enumerator.MoveNextAsync())
            {
                productList.Add(enumerator.Current);
            }
            await enumerator.DisposeAsync();
            return new OkObjectResult(productList);
        }
    }
}