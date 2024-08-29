// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Kusto;


namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Wraps around the attribute and the ingest client. Makes it easy to extract fields from KustoAttribute and ingest data provided in the attribute
    /// </summary>
    internal class KustoIngestContext
    {
        public KustoAttribute ResolvedAttribute { get; set; }

        public IKustoIngestClient IngestService { get; set; }

        private CustomIngestionProps _ingestionProperties;

        private KustoIngestionProperties GetKustoIngestionProperties(DataSourceFormat dataFormat)
        {
            this._ingestionProperties = GetIngestionProperties(this.ResolvedAttribute.IngestionProperties);

            KustoIngestionProperties kustoIngestProperties = "queued".Equals(this.ResolvedAttribute.IngestionType, StringComparison.OrdinalIgnoreCase)
                           ? new KustoQueuedIngestionProperties(this.ResolvedAttribute.Database, this.ResolvedAttribute.TableName)
                           {
                               Format = dataFormat,
                               TableName = this.ResolvedAttribute.TableName,
                               ReportLevel = IngestionReportLevel.FailuresAndSuccesses,
                               ReportMethod = IngestionReportMethod.Table,
                               FlushImmediately = this._ingestionProperties.FlushImmediately
                           }
                : new KustoIngestionProperties(this.ResolvedAttribute.Database, this.ResolvedAttribute.TableName)
                {
                    Format = dataFormat,
                    TableName = this.ResolvedAttribute.TableName
                };


            if (!string.IsNullOrEmpty(this.ResolvedAttribute.MappingRef))
            {
                var ingestionMapping = new IngestionMapping
                {
                    IngestionMappingReference = this.ResolvedAttribute.MappingRef
                };
                kustoIngestProperties.IngestionMapping = ingestionMapping;
            }
            return kustoIngestProperties;
        }

        private static CustomIngestionProps GetIngestionProperties(string IngestionProperties)
        {
            int pollTimeoutMinutes = 5; // default to 5 minutes
            int pollIntervalSeconds = 15; // default to 15 seconds
            bool flushImmediately = false;
            if (!string.IsNullOrEmpty(IngestionProperties))
            {
                IDictionary<string, object> parsedIngestionProperties = KustoBindingUtilities.ParseParameters(IngestionProperties);
                if (parsedIngestionProperties.ContainsKey("flushImmediately"))
                {
                    flushImmediately = bool.Parse(parsedIngestionProperties["flushImmediately"].ToString());
                }
                if (parsedIngestionProperties.ContainsKey("pollTimeoutMinutes"))
                {
                    pollTimeoutMinutes = int.Parse(parsedIngestionProperties["pollTimeoutMinutes"].ToString(), CultureInfo.InvariantCulture);
                }
                if (parsedIngestionProperties.ContainsKey("pollIntervalSeconds"))
                {
                    pollIntervalSeconds = int.Parse(parsedIngestionProperties["pollIntervalSeconds"].ToString(), CultureInfo.InvariantCulture);
                }
            }
            return new CustomIngestionProps(flushImmediately, pollTimeoutMinutes, pollIntervalSeconds);
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

        public async Task<IngestionStatus> IngestData(DataSourceFormat dataFormat, Stream dataToIngest, StreamSourceOptions streamSourceOptions, CancellationToken cancellationToken)
        {
            KustoIngestionProperties kustoIngestionProperties = this.GetKustoIngestionProperties(dataFormat);
            IKustoIngestionResult ingestionResult = await this.IngestService.IngestFromStreamAsync(dataToIngest, kustoIngestionProperties, streamSourceOptions);
            if ("queued".Equals(this.ResolvedAttribute.IngestionType, StringComparison.OrdinalIgnoreCase))
            {
                // Delay and poll
                return await PollIngestionStatus(ingestionResult, streamSourceOptions.SourceId, this._ingestionProperties.PollTimeoutMinutes, this._ingestionProperties.PollIntervalSeconds, cancellationToken);
            }
            else
            {
                return ingestionResult.GetIngestionStatusBySourceId(streamSourceOptions.SourceId);
            }
        }
    }

    internal sealed class CustomIngestionProps
    {
        //construtors
        // full args constructor
        public CustomIngestionProps(bool flushImmediately, int aggregationTimeoutMinutes, int pollIntervalSeconds)
        {
            this.FlushImmediately = flushImmediately;
            this.PollTimeoutMinutes = aggregationTimeoutMinutes;
            this.PollIntervalSeconds = pollIntervalSeconds;

        }
        public bool FlushImmediately { get; set; }
        public int PollTimeoutMinutes { get; set; } = 5;
        public int PollIntervalSeconds { get; set; } = 15;
    }
}