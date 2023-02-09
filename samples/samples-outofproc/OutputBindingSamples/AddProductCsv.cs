// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples
{
    internal class AddProductCsv
    {
        [Function("AddProductCsv")]
        [KustoOutput(Database: "sdktestsdb",
            TableName = "Products",
            DataFormat = "csv",
            Connection = "KustoConnectionString")]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductcsv")]
            HttpRequestData req)
        {
            Product? product = await req.ReadFromJsonAsync<Product>();
            string productString = "";
            if (!string.IsNullOrEmpty(product?.Name))
            {
                string productCsv = $"{product?.ProductID},{product?.Name},{product?.Cost}";
                productString = string.Format(CultureInfo.InvariantCulture, "(Csv : {0})", productCsv);
            }
            return productString;
        }
    }
}
