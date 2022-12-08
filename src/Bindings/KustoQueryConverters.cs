// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    internal class KustoQueryConverters
    {
        internal class KustoCslQueryConverter : IConverter<KustoAttribute, KustoQueryContext>
        {
            private readonly KustoExtensionConfigProvider _configProvider;
            private readonly ILogger _logger;
            /// <summary>
            /// Initializes a new instance of the <see cref="KustoCslQueryConverter/>"/> class.
            /// </summary>
            /// <param name="logger">ILogger used to log information and warnings</param>
            /// <param name="configProvider">KustoExtensionConfig provider that is used to initialize the client</param>
            /// <exception cref="ArgumentNullException">
            /// Thrown if the configuration is null
            /// </exception>
            public KustoCslQueryConverter(ILogger logger, KustoExtensionConfigProvider configProvider)
            {
                this._logger = logger;
                this._configProvider = configProvider;
            }

            /// <summary>
            /// </summary>
            /// <param name="attribute">
            /// Contains the KQL , Database and Connection to query the data from
            /// </param>
            /// <returns>The KustoQueryContext</returns>
            public KustoQueryContext Convert(KustoAttribute attribute)
            {
                this._logger.LogDebug("BEGIN Convert (KustoCslQueryConverter)");
                var sw = Stopwatch.StartNew();
                KustoQueryContext queryContext = this._configProvider.CreateQueryContext(attribute);
                this._logger.LogDebug($"END Convert (KustoCslQueryConverter) Duration={sw.ElapsedMilliseconds}ms");
                return queryContext;
            }
        }
        internal class KustoGenericsConverter<T> : IAsyncConverter<KustoAttribute, IEnumerable<T>>, IAsyncConverter<KustoAttribute, string>, IAsyncConverter<KustoAttribute, JArray>, IConverter<KustoAttribute, IAsyncEnumerable<T>>
        {
            private readonly KustoExtensionConfigProvider _configProvider;
            private readonly ILogger _logger;
            public KustoGenericsConverter(ILogger logger, KustoExtensionConfigProvider configProvider)
            {
                this._logger = logger;
                this._configProvider = configProvider;
            }

            /// <summary>
            /// Get matching records into a list and return it
            /// </summary>
            /// <param name="attribute">The attribute that contains the query and parameters for teh query</param>
            /// <param name="cancellationToken">The async cancellation token</param>
            /// <returns>A list of T , the type of the object retrieved</returns>
            public async Task<IEnumerable<T>> ConvertAsync(KustoAttribute attribute, CancellationToken cancellationToken)
            {
                this._logger.LogDebug("BEGIN ConvertAsync (IEnumerable)");
                var sw = Stopwatch.StartNew();
                try
                {
                    List<T> results = (await BuildJsonArrayFromAttributeAsync(attribute, this._configProvider)).ToObject<List<T>>();
                    this._logger.LogDebug($"END ConvertAsync (IEnumerable) Duration={sw.ElapsedMilliseconds}ms");
                    return results;
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error in ConvertAsync - EnumerableType ");
                    throw;
                }
            }
            /// <summary>
            /// Get matching records as a string and return it
            /// </summary>
            /// <param name="attribute">The attribute that contains the query and parameters for teh query</param>
            /// <param name="cancellationToken">The async cancellation token</param>
            /// <returns>A string (array) that contains the string representation</returns>
            async Task<string> IAsyncConverter<KustoAttribute, string>.ConvertAsync(KustoAttribute attribute, CancellationToken cancellationToken)
            {
                this._logger.LogDebug("BEGIN ConvertAsync (string)");
                var sw = Stopwatch.StartNew();
                string result = (await BuildJsonArrayFromAttributeAsync(attribute, this._configProvider)).ToString();
                this._logger.LogDebug($"END ConvertAsync (string) Duration={sw.ElapsedMilliseconds}ms");
                return result;
            }
            /// <summary>
            /// Get matching JSON Array for processing
            /// </summary>
            /// <param name="attribute">The attribute that contains the query and parameters for teh query</param>
            /// <param name="cancellationToken">The async cancellation token</param>
            /// <returns>A JSON Array that contains the list of retrieved records</returns>
            async Task<JArray> IAsyncConverter<KustoAttribute, JArray>.ConvertAsync(KustoAttribute attribute, CancellationToken cancellationToken)
            {
                this._logger.LogDebug("BEGIN ConvertAsync (JArray)");
                var sw = Stopwatch.StartNew();
                JArray result = await BuildJsonArrayFromAttributeAsync(attribute, this._configProvider);
                this._logger.LogDebug($"END ConvertAsync (JArray) Duration={sw.ElapsedMilliseconds}ms");
                return result;
            }
            /// <summary>
            /// Provide an async implementation of collecting retrieved objects as a list
            /// </summary>
            /// <param name="attribute"></param>
            /// <returns>A list of T , the type of the object retrieved (async)</returns>
            public IAsyncEnumerable<T> Convert(KustoAttribute attribute)
            {
                KustoQueryContext context = this._configProvider.CreateQueryContext(attribute);
                return new KustoAsyncEnumerable<T>(context);
            }
        }
        private static async Task<JArray> BuildJsonArrayFromAttributeAsync(KustoAttribute attribute, KustoExtensionConfigProvider configProvider)
        {
            KustoQueryContext kustoQueryContext = configProvider.CreateQueryContext(attribute);
            string tracingRequestId = Guid.NewGuid().ToString();
            ClientRequestProperties clientRequestProperties;
            if (!string.IsNullOrEmpty(attribute.KqlParameters))
            {
                // expect that this is a JSON in a specific format
                // We expect that we have a declarative query mechanism to perform KQL
                IDictionary<string, string> queryParameters = KustoBindingUtilities.ParseParameters(attribute.KqlParameters);
                clientRequestProperties = new ClientRequestProperties(options: null, parameters: queryParameters)
                {
                    ClientRequestId = $"{KustoConstants.ClientRequestId};{tracingRequestId}",
                };
            }
            else
            {
                clientRequestProperties = new ClientRequestProperties()
                {
                    ClientRequestId = $"{KustoConstants.ClientRequestId};{tracingRequestId}",
                };
            }
            Task<IDataReader> queryTask = kustoQueryContext.QueryProvider.ExecuteQueryAsync(attribute.Database, attribute.KqlCommand, clientRequestProperties);
            var jArray = new JArray();
            using (IDataReader queryReader = await queryTask.ConfigureAwait(false))
            {
                if (queryReader != null)
                {
                    using (queryReader)
                    {
                        queryReader.ToJObjects().ForEach(jObject => jArray.Add(jObject));
                    }
                }
            }
            return jArray;
        }
    }
}
