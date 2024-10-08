﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Kusto
{
    /// <summary>
    /// Provides a holder to add multiple items of the generic type T through Add and then provide a way to flush them 
    /// in a single batch using Flush
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class KustoAsyncCollector<T> : IAsyncCollector<T>, IDisposable
    {
        //A list of rows to ingest
        private readonly List<T> _rows = new List<T>();
        // Used to co-ordinate writes and flushes.
        private readonly SemaphoreSlim _rowLock = new SemaphoreSlim(1, 1);
        private readonly KustoIngestContext _kustoIngestContext;
        private readonly ILogger _logger;
        private readonly Lazy<string> _contextdetail;


        public KustoAsyncCollector(KustoIngestContext kustoContext, ILogger logger)
        {
            this._kustoIngestContext = kustoContext;
            this._logger = logger;
            this._contextdetail = new Lazy<string>(() => $"TableName='{kustoContext.ResolvedAttribute?.TableName}'," +
            $"Database='{kustoContext.ResolvedAttribute?.Database}', " +
            $"MappingRef='{kustoContext.ResolvedAttribute?.MappingRef}', " +
            $"DataFormat='{this.GetDataFormat()}', " +
            $"IngestionType='{kustoContext.ResolvedAttribute?.IngestionType}', " +
            $"IngestionProperties='{kustoContext.ResolvedAttribute?.IngestionProperties}', " +
            $"ManagedIdentity='{kustoContext.ResolvedAttribute?.ManagedServiceIdentity}'");
        }

        /// <summary>
        /// Adds an item to this collector that is processed in a batch along with all other items added via
        /// AddAsync when <see cref="FlushAsync"/> is called. Each item is interpreted as a row to be added to the Kusto table
        /// specified in the Binding.
        /// </summary>
        /// <param name="item"> The item to add to the collector.</param>
        /// <param name="cancellationToken">The cancellationToken is not used in this method.</param>
        /// <returns> A CompletedTask if executed successfully.</returns>
        public async Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            if (item != null)
            {
                await this._rowLock.WaitAsync(cancellationToken);
                try
                {
                    this._rows.Add(item);
                }
                finally
                {
                    this._rowLock.Release();
                }
            }
        }

        /// <summary>
        /// Ingest rows to be added into the Kusto table. This uses managed streaming for the ingestion <see cref="AddAsync"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken is not used in this method.</param>
        /// <returns> A CompletedTask if executed successfully. If no rows were added, this is returned
        /// automatically. </returns>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            await this._rowLock.WaitAsync(cancellationToken);
            if (this._logger.IsEnabled(LogLevel.Debug))
            {
                this._logger.LogDebug($"Flushing rows with {this._rows.Count} rows to ingest.Ingest detail {this._contextdetail.Value}");
            }
            var ingestSourceId = Guid.NewGuid();
            try
            {
                if (this._rows.Count != 0)
                {
                    IngestionStatus ingestionStatus = await this.IngestRowsAsync(ingestSourceId, cancellationToken);
                    if (ingestionStatus.Status == Status.Failed || ingestionStatus.Status == Status.PartiallySucceeded)
                    {
                        string errorMessage = $"Ingestion status reported failure/partial success for {ingestSourceId}. Ingest detail {this._contextdetail.Value}, and status reported was {ingestionStatus.Status}";
                        this._logger.LogError(errorMessage);
                        throw new FunctionInvocationException(errorMessage);
                    }
                    this._rows.Clear();
                }
            }
            catch (Exception ex)
            {
                // Once we have the blob Id all the attributes of DataIngestPull can then be retrieved (format,metadata about the ingest etc.)
                this._logger.LogError("Exception ingesting rows with SourceId {IngestSourceId}. Ingest detail {IngestDetail}", ingestSourceId.ToString(), this._contextdetail.Value, ex);
                throw;
            }
            finally
            {
                this._rowLock.Release();
            }
        }

        /// <summary>
        /// Performs the actual ingestion using managed ingest client
        /// </summary>
        /// <param name="ingestSourceId">The ingest source id is used to track the ingestion</param>
        /// <returns></returns>
        private async Task<IngestionStatus> IngestRowsAsync(Guid ingestSourceId, CancellationToken cancellationToken = default)
        {
            DataSourceFormat format = this.GetDataFormat();
            string dataToIngest = (format == DataSourceFormat.multijson || format == DataSourceFormat.json) ? this.SerializeToIngestData() : string.Join(Environment.NewLine, this._rows);
            var streamSourceOptions = new StreamSourceOptions()
            {
                SourceId = ingestSourceId,
            };
            return await this.IngestData(dataToIngest, format, streamSourceOptions, cancellationToken);
        }

        private async Task<IngestionStatus> IngestData(string dataToIngest, DataSourceFormat format, StreamSourceOptions streamSourceOptions, CancellationToken cancellationToken)
        {
            bool isQueuedIngestion = "queued".EqualsOrdinalIgnoreCase(this._kustoIngestContext.ResolvedAttribute.IngestionType);
            if (this._logger.IsEnabled(LogLevel.Debug))
            {
                this._logger.LogDebug($"Ingesting data with SourceId {streamSourceOptions.SourceId} using {this._kustoIngestContext.ResolvedAttribute.IngestionType} ingestion");
            }
            IKustoIngestionService ingestionService = isQueuedIngestion
                ? new KustoQueuedIngestionService(this._kustoIngestContext, this._logger)
                : (IKustoIngestionService)new KustoManagedIngestionService(this._kustoIngestContext, this._logger);
            return await ingestionService.IngestData(format, KustoBindingUtilities.StreamFromString(dataToIngest), streamSourceOptions, cancellationToken);
        }

        private string SerializeToIngestData()
        {
            var sb = new StringBuilder();
            bool first = true;
            Formatting indent = this._rows.Count == 1 ? Formatting.None : Formatting.Indented;
            using (var textWriter = new StringWriter(sb))
            {
                foreach (T row in this._rows)
                {
                    if (!first)
                    {
                        textWriter.WriteLine(string.Empty);
                    }
                    first = false;
                    using var jsonWriter = new JsonTextWriter(textWriter) { QuoteName = true, Formatting = indent, CloseOutput = false };
                    if (typeof(T) == typeof(string))
                    {
                        textWriter.Write(row.ToString());
                    }
                    else
                    {
                        // POCO , JObject
                        JsonSerializer.CreateDefault().Serialize(jsonWriter, row);
                    }
                }
            }
            return sb.ToString();
        }

        private DataSourceFormat GetDataFormat()
        {
            KustoAttribute resolvedAttribute = this._kustoIngestContext.ResolvedAttribute;
            DataSourceFormat returnFormat = DataSourceFormat.json;
            if (string.IsNullOrEmpty(resolvedAttribute.DataFormat))
            {
                if (this._rows.Count > 1)
                {
                    returnFormat = DataSourceFormat.multijson;
                }
            }
            else
            {
                bool parseResult = Enum.TryParse(resolvedAttribute.DataFormat, out DataSourceFormat ingestDataFormat);
                // If user provides JSON and it has multiple values then convert to multi-json
                returnFormat = parseResult && ingestDataFormat == DataSourceFormat.json && this._rows.Count > 1
                    ? DataSourceFormat.multijson
                    : ingestDataFormat;

            }
            return returnFormat;
        }

        public void Dispose()
        {
            this._rows.Clear();
            this._rowLock.Dispose();
        }
    }
}