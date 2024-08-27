// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Kusto.Ingest;


namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Wraps around the attribute and the ingest client. Makes it easy to extract fields from KustoAttribute and ingest data provided in the attribute
    /// </summary>
    internal class KustoIngestContext
    {
        public WebJobs.Kusto.KustoAttribute ResolvedAttribute { get; set; }

        public IKustoIngestClient IngestService { get; set; }

        private KustoIngestionProperties GetKustoIngestionProperties(DataSourceFormat dataFormat)
        {
            KustoIngestionProperties kustoIngestProperties = "queued".Equals(this.ResolvedAttribute.IngestionType, StringComparison.OrdinalIgnoreCase)
                           ? new KustoQueuedIngestionProperties(this.ResolvedAttribute.Database, this.ResolvedAttribute.TableName)
                           {
                               Format = dataFormat,
                               TableName = this.ResolvedAttribute.TableName,
                               ReportLevel = IngestionReportLevel.FailuresAndSuccesses,
                               ReportMethod = IngestionReportMethod.Table
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

        private static async Task<IngestionStatus> PollIngestionStatus(IKustoIngestionResult queuedIngestResult, Guid sourceId, CancellationToken cancellationToken)
        {
            IngestionStatus ingestionStatus = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                ingestionStatus = queuedIngestResult.GetIngestionStatusBySourceId(sourceId);
                // Check if the ingestion status indicates completion
                if (ingestionStatus.Status != Status.Pending)
                {
                    break;
                }
                // Wait for a specified interval before polling again
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return ingestionStatus;
        }

        public async Task<IngestionStatus> IngestData(DataSourceFormat dataFormat, System.IO.Stream dataToIngest, StreamSourceOptions streamSourceOptions, CancellationToken cancellationToken)
        {
            KustoIngestionProperties kustoIngestionProperties = this.GetKustoIngestionProperties(dataFormat);
            IKustoIngestionResult ingestionResult = await this.IngestService.IngestFromStreamAsync(dataToIngest, kustoIngestionProperties, streamSourceOptions);
            IngestionStatus ingestionStatus = ingestionResult.GetIngestionStatusBySourceId(streamSourceOptions.SourceId);
            if ("queued".Equals(this.ResolvedAttribute.IngestionType, StringComparison.OrdinalIgnoreCase))
            {
                // Delay and poll
                if (ingestionStatus.Status == Status.Pending)
                {
                    return await PollIngestionStatus(ingestionResult, streamSourceOptions.SourceId, cancellationToken);
                }
            }
            return ingestionStatus;
        }
    }
}