﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Kusto;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.SamplesOutOfProc.InputBindingSamples
{
    public static class GetProducts
    {
        [Function("GetProducts")]
        public static JsonObject Run(
#pragma warning disable
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproducts/{productId}")] HttpRequestData req,
#pragma warning disable
            [KustoInput(Database: "sdktestsdb",
            KqlCommand = "declare query_parameters (productId:long);Products | where ProductID == productId",
            KqlParameters = "@productId={productId}",Connection = "KustoConnectionString")] JsonObject products)
        {
            return products;
        }
    }
}
