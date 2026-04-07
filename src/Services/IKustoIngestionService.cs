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

        /// <summary>
        /// Polls the ingestion status until a terminal state is reached or timeout occurs.
        /// Used by both queued ingestion and managed ingestion when it falls back to queued.
        /// </summary>
        protected static async Task<IngestionStatus> PollIngestionStatus(IKustoIngestionResult queuedIngestResult, Guid sourceId, int ingestionTimeoutMinutes, int pollIntervalSeconds, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(ingestionTimeoutMinutes));
            IngestionStatus ingestionStatus = null;
            while (!cts.Token.IsCancellationRequested)
            {
                ingestionStatus = queuedIngestResult.GetIngestionStatusBySourceId(sourceId);
                if (ingestionStatus.Status == Status.Succeeded
                    || ingestionStatus.Status == Status.Skipped
                    || ingestionStatus.Status == Status.PartiallySucceeded
                    || ingestionStatus.Status == Status.Failed)
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cts.Token);
            }
            if (ingestionStatus == null)
            {
                cts.Token.ThrowIfCancellationRequested();
            }
            return ingestionStatus;
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
            // When managed streaming ingestion falls back to queued, the immediate status is Queued/Pending.
            // In this case, poll for the final status to ensure we catch permission and ingestion errors.
            if (managedIngestionStatus.Status == Status.Queued || managedIngestionStatus.Status == Status.Pending)
            {
                this._logger.LogDebug($"Managed ingestion fell back to queued for sourceId {streamSourceOptions.SourceId}. Polling for final status.");
                return await PollIngestionStatus(ingestionResult, streamSourceOptions.SourceId, 1, 5, cancellationToken);
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
            bool flushImmediately = false;
            if (ingestionPropertiesDict.TryGetValue("flushImmediately", out object flushImmediatelyObj))
            {
                if (!bool.TryParse(flushImmediatelyObj.ToString(), out flushImmediately))
                {
                    // Handle parsing failure, e.g., log an error or set a default value
                    flushImmediately = false; // Default value
                }
            }

            int pollIntervalSeconds = 30;
            if (ingestionPropertiesDict.TryGetValue("pollIntervalSeconds", out object pollIntervalSecondsObj))
            {
                int.TryParse(pollIntervalSecondsObj.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out pollIntervalSeconds);
            }

            int pollTimeoutMinutes = 30;
            if (ingestionPropertiesDict.TryGetValue("pollTimeoutMinutes", out object pollTimeoutMinutesObj))
            {
                int.TryParse(pollTimeoutMinutesObj.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out pollTimeoutMinutes);
            }

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
    }
}