// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    internal class AddProductCsv
    {
        [FunctionName("AddProductCsv")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductcsv")]
            HttpRequest req, ILogger log,
            [Kusto(Database:SampleConstants.DatabaseName ,
            TableName =SampleConstants.ProductsTable ,
            DataFormat = "csv",
            Connection = "KustoConnectionString")] out string productCsv)
        {
            productCsv = new StreamReader(req.Body).ReadToEnd();
            string productString = string.Format(CultureInfo.InvariantCulture, "(Csv : {0})", productCsv);
            log.LogInformation("Ingested product CSV {}", productString);
            return new CreatedResult($"/api/addproductcsv", productString);
        }
    }
}
