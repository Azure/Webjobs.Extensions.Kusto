// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Azure.WebJobs.Extensions.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Kusto
{
    internal class KustoAsyncCollectorBuilder<T> : IConverter<KustoAttribute, IAsyncCollector<T>>
    {
        private readonly KustoExtensionConfigProvider _configProvider;

        private readonly ILogger _logger;

        /// <summary>
        /// Use the builder to create the async collector that is used to read and collect multiple items. If we get a multiple input scenaio
        /// we want to Add them into a collection and then flush it once (than ingesting on a per item basis)
        /// </summary>
        /// <param name="logger">The logger to log in the collector (if there are any binding failures)</param>
        /// <param name="configProvider">Config provider to create the ingestion context and initialize the client</param>
        public KustoAsyncCollectorBuilder(ILogger logger, KustoExtensionConfigProvider configProvider)
        {
            this._logger = logger;
            this._configProvider = configProvider;
        }

        /// <summary>
        /// Get the collector to read / collect
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns><see cref="IAsyncCollector"/></returns>
        IAsyncCollector<T> IConverter<KustoAttribute, IAsyncCollector<T>>.Convert(KustoAttribute attribute)
        {
            this._logger.LogDebug("BEGIN Convert (KustoAsyncCollectorBuilder)");
            var sw = Stopwatch.StartNew();
            KustoIngestContext context = this._configProvider.CreateIngestionContext(attribute);
            this._logger.LogDebug($"END Convert (KustoAsyncCollectorBuilder) Duration={sw.ElapsedMilliseconds}ms");
            return new KustoAsyncCollector<T>(context);
        }
    }
}