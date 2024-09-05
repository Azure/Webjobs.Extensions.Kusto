// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.InputBindingSamples
{
    public static class GetProductsFunctionManagedIdentity
    {
        [FunctionName("GetProductsFunctionManagedIdentity")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproductsmi/{name}")]
#pragma warning disable IDE0060 // Remove unused parameter
            HttpRequest req,
#pragma warning restore IDE0060 // Remove unused parameter
            [Kusto(Database:SampleConstants.DatabaseName ,
            KqlCommand = "declare query_parameters (name:string);GetProductsByName(name)" ,
            KqlParameters = "@name={name}",
            Connection = "KustoConnectionStringManagedIdentity", ManagedServiceIdentity ="ed501a5f-e46e-4ca0-9bc0-27a46c7799ff")]
            IEnumerable<Product> products)
        {
            return new OkObjectResult(products);
        }
    }
}