// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples
{
    internal class AddProductJson
    {
        [Function("AddProductJson")]
        [KustoOutput(Database: "sdktestsdb",
            TableName = "Products",
            DataFormat = "csv",
            Connection = "KustoConnectionString")]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductjson")]
            HttpRequestData req)
        {
            string? product = await req.ReadAsStringAsync();
            return product ?? "";
        }
    }
}
