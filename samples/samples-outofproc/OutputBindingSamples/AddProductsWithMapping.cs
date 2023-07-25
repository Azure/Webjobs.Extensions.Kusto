// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples
{
    public static class AddProductsWithMapping
    {
        [Function("AddProductsWithMapping")]
        [KustoOutput(Database: SampleConstants.DatabaseName, Connection = "KustoConnectionString", TableName = SampleConstants.ProductsTable, MappingRef = "item_to_product_json")]
        public static async Task<Item> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductswithmapping")]
            HttpRequestData req)
        {
            Item? item = await req.ReadFromJsonAsync<Item>();
            return item ?? new Item { };
        }
    }
}