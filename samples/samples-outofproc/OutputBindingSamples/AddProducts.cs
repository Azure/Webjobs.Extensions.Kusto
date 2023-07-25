// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples
{
    public static class AddProducts
    {
        [Function("AddProducts")]
        [KustoOutput(Database: SampleConstants.DatabaseName, Connection = "KustoConnectionString", TableName = SampleConstants.ProductsTable)]
        public static async Task<Product[]> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproduct")]
            HttpRequestData req)
        {
            Product[]? products = await req.ReadFromJsonAsync<Product[]>();
            return products ?? Array.Empty<Product>();
        }
    }
}