// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Kusto.Ingest;
using Newtonsoft.Json;


namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Wraps around the attribute and the ingest client. Makes it easy to extract fields from KustoAttribute and ingest data provided in the attribute
    /// </summary>
    internal class KustoIngestContext
    {
        public WebJobs.Kusto.KustoAttribute ResolvedAttribute { get; set; }

        public IKustoIngestClient IngestService { get; set; }

        private CustomIngestionProps _ingestionProperties;

        private KustoIngestionProperties GetKustoIngestionProperties(DataSourceFormat dataFormat)
        {
            this._ingestionProperties = string.IsNullOrEmpty(this.ResolvedAttribute.IngestionPropertiesJson)
                ? new CustomIngestionProps()
                : JsonConvert.DeserializeObject<CustomIngestionProps>(this.ResolvedAttribute.IngestionPropertiesJson);

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

        private static async Task<IngestionStatus> PollIngestionStatus(IKustoIngestionResult queuedIngestResult, Guid sourceId, int ingestionTimeoutMinutes, CancellationToken cancellationToken)
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
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
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
                return await PollIngestionStatus(ingestionResult, streamSourceOptions.SourceId, this._ingestionProperties.PollTimeoutMinutes, cancellationToken);
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
        public CustomIngestionProps()
        {
            this.FlushImmediately = false;
            this.PollTimeoutMinutes = 5; // default to 5 minutes
        }
        // full args constructor
        public CustomIngestionProps(bool flushImmediately, int aggregationTimeoutMinutes)
        {
            this.FlushImmediately = flushImmediately;
            this.PollTimeoutMinutes = aggregationTimeoutMinutes;
        }
        public bool FlushImmediately { get; set; }
        public int PollTimeoutMinutes { get; set; } = 5;
    }
}