// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.OutputBindingSamples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.InputBindingSamples
{
    public static class GetProductsFunction
    {
        [Function("GetProductsFunction")]
        public static IEnumerable<Product> Run(
#pragma warning disable
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproductsfn/{name}")] HttpRequestData req,
#pragma warning disable
            [KustoInput(Database: SampleConstants.DatabaseName,
            KqlCommand = "declare query_parameters (name:string);GetProductsByName(name)",
            KqlParameters = "@name={name}",Connection = "KustoConnectionString")] IEnumerable<Product> products)
        {
            return products;
        }
    }
}