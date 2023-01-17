// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples
{
    public static class AddProduct
    {
        [Function("AddProduct")]
        [KustoOutput("dbo.Products", Connection = "KustoConnectionString", TableName = "Products")]
        public static async Task<Product> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "oop-addproduct")]
            HttpRequestData req)
        {
            Product prod = await req.ReadFromJsonAsync<Product>();
            return prod;
        }
    }
}
