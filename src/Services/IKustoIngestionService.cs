// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Wrap around Kusto internal classes and provides a mechanism to provide the query and ingest clients. Has an additional benefit that
    /// testing is a lot easier
    /// </summary>
    internal abstract class IKustoIngestionService
    {
        public abstract Task<IngestionStatus> IngestData(DataSourceFormat dataFormat, Stream dataToIngest, StreamSourceOptions streamSourceOptions, CancellationToken cancellationToken);

        public static KustoIngestionProperties GetKustoIngestionProperties(DataSourceFormat dataFormat, KustoAttribute resolvedAttribute, bool isQueuedIngestion = false)
        {

            KustoIngestionProperties kustoIngestProperties = isQueuedIngestion
                ? new KustoQueuedIngestionProperties(resolvedAttribute.Database, resolvedAttribute.TableName)
                {
                    Format = dataFormat,
                    TableName = resolvedAttribute.TableName,
                    ReportLevel = IngestionReportLevel.FailuresAndSuccesses,
                    ReportMethod = IngestionReportMethod.Table
                }
                : new KustoIngestionProperties(resolvedAttribute.Database, resolvedAttribute.TableName)
                {
                    Format = dataFormat,
                    TableName = resolvedAttribute.TableName
                };
            if (!string.IsNullOrEmpty(resolvedAttribute.MappingRef))
            {
                var ingestionMapping = new IngestionMapping
                {
                    IngestionMappingReference = resolvedAttribute.MappingRef
                };
                kustoIngestProperties.IngestionMapping = ingestionMapping;
            }
            return kustoIngestProperties;
        }
    }

    internal class KustoManagedIngestionService : IKustoIngestionService
    {
        private readonly KustoIngestContext _ingestionContext;
        private readonly ILogger _logger;

        public KustoManagedIngestionService(KustoIngestContext ingestionContext, ILogger logger)
        {
            this._ingestionContext = ingestionContext;
            this._logger = logger;
        }

        public override async Task<IngestionStatus> IngestData(DataSourceFormat dataFormat, Stream dataToIngest, StreamSourceOptions streamSourceOptions, CancellationToken cancellationToken)
        {
            KustoIngestionProperties ingestionProperties = GetKustoIngestionProperties(dataFormat, this._ingestionContext.ResolvedAttribute, false);
            IKustoIngestionResult ingestionResult = await this._ingestionContext.IngestService.IngestFromStreamAsync(dataToIngest, ingestionProperties, streamSourceOptions);
            IngestionStatus managedIngestionStatus = ingestionResult.GetIngestionStatusBySourceId(streamSourceOptions.SourceId);
            if (this._logger.IsEnabled(LogLevel.Debug))
            {
                this._logger.LogDebug($"Ingestion status for sourceId {streamSourceOptions.SourceId} is {managedIngestionStatus.Status}");
            }
            return managedIngestionStatus;
        }
    }

    internal class KustoQueuedIngestionService : IKustoIngestionService
    {
        private readonly KustoIngestContext _ingestionContext;
        private readonly ILogger _logger;

        public KustoQueuedIngestionService(KustoIngestContext ingestionContext, ILogger logger)
        {
            this._ingestionContext = ingestionContext;
            this._logger = logger;
        }

        public override async Task<IngestionStatus> IngestData(DataSourceFormat dataFormat, Stream dataToIngest, StreamSourceOptions streamSourceOptions, CancellationToken cancellationToken)
        {
            var ingestionProperties = (KustoQueuedIngestionProperties)GetKustoIngestionProperties(dataFormat, this._ingestionContext.ResolvedAttribute, true);
            System.Collections.Generic.IDictionary<string, object> ingestionPropertiesDict = KustoBindingUtilities.ParseParameters(this._ingestionContext.ResolvedAttribute.IngestionProperties);
            bool flushImmediately = ingestionPropertiesDict.ContainsKey("flushImmediately") && bool.Parse(ingestionPropertiesDict["flushImmediately"].ToString());
            int pollIntervalSeconds = ingestionPropertiesDict.ContainsKey("pollIntervalSeconds") ? int.Parse(ingestionPropertiesDict["pollIntervalSeconds"].ToString(), CultureInfo.InvariantCulture) : 30;
            int pollTimeoutMinutes = ingestionPropertiesDict.ContainsKey("pollTimeoutMinutes") ? int.Parse(ingestionPropertiesDict["pollTimeoutMinutes"].ToString(), CultureInfo.InvariantCulture) : 30;
            if (flushImmediately)
            {
                this._logger.LogWarning($"Flush immediately has been set for  {streamSourceOptions.SourceId}. No aggregation will be performed for ingestion. This is not recommended for large data sets");
                ingestionProperties.FlushImmediately = flushImmediately;
            }
            IKustoIngestionResult ingestionResult = await this._ingestionContext.IngestService.IngestFromStreamAsync(dataToIngest, ingestionProperties, streamSourceOptions);
            if (this._logger.IsEnabled(LogLevel.Trace))
            {
                string logString = $"Additional properties passed {ingestionProperties.FlushImmediately} , Will poll every {pollIntervalSeconds} for status, until {pollTimeoutMinutes} minutes elapse";
                this._logger.LogTrace($"Queued ingestion for sourceId {streamSourceOptions.SourceId}. Using ingestion properties {logString}");
            }
            return await PollIngestionStatus(ingestionResult, streamSourceOptions.SourceId, pollTimeoutMinutes, pollIntervalSeconds, cancellationToken);
        }

        private static async Task<IngestionStatus> PollIngestionStatus(IKustoIngestionResult queuedIngestResult, Guid sourceId, int ingestionTimeoutMinutes, int pollIntervalSeconds, CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(ingestionTimeoutMinutes));
            IngestionStatus ingestionStatus = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                ingestionStatus = queuedIngestResult.GetIngestionStatusBySourceId(sourceId);
                // Check if the ingestion status indicates completion
                if (ingestionStatus.Status == Status.Succeeded
                    || ingestionStatus.Status == Status.Skipped // The ingestion was skipped because it was already ingested 
                    || ingestionStatus.Status == Status.PartiallySucceeded // Some of the records were ingested 
                    || ingestionStatus.Status == Status.Failed)
                {
                    break;
                }
                // Wait for a specified interval before polling again
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken);
            }
            return ingestionStatus;
        }
    }
}