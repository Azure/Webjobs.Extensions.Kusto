﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.InputBindingSamples
{
    public static class GetProductsString
    {
        [FunctionName("GetProductsString")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproducts-string/{productId}")]
#pragma warning disable IDE0060 // Remove unused parameter
            HttpRequest req,
#pragma warning restore IDE0060 // Remove unused parameter
            [Kusto(Database:SampleConstants.DatabaseName ,
            KqlCommand = "declare query_parameters (productId:long);Products | where ProductID == productId" ,
            KqlParameters = "@productId={productId}",
            Connection = "KustoConnectionString")]
            string products)
        {
            return new OkObjectResult(products);
        }
    }
}