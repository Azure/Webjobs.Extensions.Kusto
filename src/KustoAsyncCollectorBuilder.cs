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


        public KustoAsyncCollectorBuilder(ILogger logger, KustoExtensionConfigProvider configProvider)
        {
            this._logger = logger;
            this._configProvider = configProvider;
        }

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