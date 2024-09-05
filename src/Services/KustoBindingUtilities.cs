// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Kusto
{
    internal static class KustoBindingUtilities
    {
        public static Stream StreamFromString(string dataToIngest)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(dataToIngest);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }


        public static string SerializeRow(IDataReader reader)
        {
            return JsonConvert.SerializeObject(Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, i => reader.GetValue(i)));
        }

        public static IDictionary<string, object> ParseParameters(string parameters)
        {
            IDictionary<string, object> kvParameters = new Dictionary<string, object>();
            // If parameters is null, user did not specify any parameters in their function so nothing to parse
            if (!string.IsNullOrEmpty(parameters))
            {
                // Because we remove empty entries, we will ignore any commas that appear at the beginning/end of the parameter list,
                // as well as extra commas that appear between parameter pairs.
                // I.e., ",,@param1=param1,,@param2=param2,,," will be parsed just like "@param1=param1,@param2=param2" is.
                string[] paramPairs = parameters.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string pair in paramPairs)
                {
                    // Note that we don't throw away empty entries here, so a parameter pair that looks like "=@param1=param1"
                    // or "@param2=param2=" is considered malformed
                    string[] items = pair.Split('=');
                    if (items.Length != 2)
                    {
                        throw new ArgumentException("Parameters must be separated by \",\" and parameter name and parameter value must be separated by \"=\", " +
                           "i.e. \"@param1=param1,@param2=param2\". To specify a null value, use null, as in \"@param1=null,@param2=param2\"." +
                           "To specify an empty string as a value, simply do not add anything after the equals sign, as in \"@param1=,@param2=param2\".");
                    }
                    string key = items[0].Trim();
                    if (!key.StartsWith("@", StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new ArgumentException("Parameter name must start with \"@\", i.e. \"@param1=param1,@param2=param2\"");
                    }
                    kvParameters.Add(key[1..], items[1]);
                }
            }
            return kvParameters;
        }
    }
}