// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Data;
using Kusto.Data.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

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
        internal class KustoGenericsConverter<T> : IAsyncConverter<KustoAttribute, IEnumerable<T>>, IConverter<KustoAttribute, IAsyncEnumerable<T>>
        {
            private readonly KustoExtensionConfigProvider _configProvider;
            private readonly ILogger _logger;
            public KustoGenericsConverter(ILogger logger, KustoExtensionConfigProvider configProvider)
            {
                this._logger = logger;
                this._configProvider = configProvider;
            }

            public async Task<IEnumerable<T>> ConvertAsync(KustoAttribute attribute, CancellationToken cancellationToken)
            {
                this._logger.LogDebug("BEGIN ConvertAsync (IEnumerable)");
                var sw = Stopwatch.StartNew();
                try
                {
                    IEnumerable<T> results = await this.BuildItemFromAttributeAsync(attribute);
                    this._logger.LogDebug($"END ConvertAsync (IEnumerable) Duration={sw.ElapsedMilliseconds}ms");
                    return results;
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error in ConvertAsync - EnumerableType ");
                    throw;
                }
            }
            public virtual async Task<IEnumerable<T>> BuildItemFromAttributeAsync(KustoAttribute attribute)
            {
                KustoQueryContext kustoQueryContext = this._configProvider.CreateQueryContext(attribute);
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
                using (IDataReader queryReader = await queryTask.ConfigureAwait(false))
                {
                    using (queryReader)
                    {
                        return queryReader.ToJObjects().Select(jObject => jObject.ToObject<T>()).ToList();
                    }
                }
            }

            public IAsyncEnumerable<T> Convert(KustoAttribute attribute)
            {
                KustoQueryContext context = this._configProvider.CreateQueryContext(attribute);
                return new KustoAsyncEnumerable<T>(context);
            }
        }
    }
}
