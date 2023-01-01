// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Samples.OutputBindingSamples
{
    internal class AddProductJson
    {
        [FunctionName("AddProductJson")]
        public static void Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductjson")]
            HttpRequest req, ILogger log,
            [Kusto(database:"sdktestsdb" ,
            TableName ="Products" ,
            Connection = "KustoConnectionString")] out JObject json)
        {
            string body = new StreamReader(req.Body).ReadToEnd();
            json = JObject.Parse(body);
            string productString = string.Format(CultureInfo.InvariantCulture, "(JSON : {0})", json);
            log.LogInformation("Ingested product JSON {}", productString);
        }
    }
}
