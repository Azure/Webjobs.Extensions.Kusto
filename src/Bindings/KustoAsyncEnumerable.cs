// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Provides a mechanism to get an AsyncEnumerable. Useful in scenarios where a large number of records match
    /// the input query predicate and the results have to be streamed async
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class KustoAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly KustoQueryContext _kustoQueryContext;
        public KustoAsyncEnumerable(KustoQueryContext kustoQueryContext)
        {
            this._kustoQueryContext = kustoQueryContext ?? throw new ArgumentNullException(nameof(kustoQueryContext));
        }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new KustoAsyncEnumerator(this._kustoQueryContext);
        }

        private class KustoAsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly KustoQueryContext _kustoQueryContext;
            private IDataReader _reader;
            public KustoAsyncEnumerator(KustoQueryContext kustoQueryContext)
            {
                this._kustoQueryContext = kustoQueryContext ?? throw new ArgumentNullException(nameof(kustoQueryContext));
            }
            /// <summary>
            /// Get the current row being processed
            /// </summary>
            public T Current { get; private set; }

            public ValueTask DisposeAsync()
            {
                this._reader?.Close();
                return new ValueTask(Task.CompletedTask);
            }

            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(this.GetNextRowAsync());
            }
            /// <summary>
            /// Assign the current record by moving ahead in the stream. The result of the Query in ADX is an IDataReader that can be
            /// read from and individual records be assigned
            /// </summary>
            /// <returns></returns>
            private async Task<bool> GetNextRowAsync()
            {
                if (this._kustoQueryContext.QueryProvider != null)
                {
                    if (this._reader == null)
                    {
                        string tracingRequestId = Guid.NewGuid().ToString();
                        // expect that this is a string in a specific format
                        // We expect that we have a declarative query mechanism to perform KQL
                        IEnumerable<KeyValuePair<string, string>> queryParameters = KustoBindingUtilities.ParseParameters(this._kustoQueryContext.ResolvedAttribute.KqlParameters).Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString()));
                        IEnumerable<KeyValuePair<string, object>> crpOptions = KustoBindingUtilities.ParseParameters(this._kustoQueryContext.ResolvedAttribute.ClientRequestProperties).Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                        var clientRequestProperties = new ClientRequestProperties(options: crpOptions, parameters: queryParameters)
                        {
                            ClientRequestId = $"{KustoConstants.ClientRequestId};{tracingRequestId}",
                        };
                        this._reader = await this._kustoQueryContext.QueryProvider.ExecuteQueryAsync(this._kustoQueryContext.ResolvedAttribute.Database, this._kustoQueryContext.ResolvedAttribute.KqlCommand, clientRequestProperties);
                    }
                    if (this._reader.Read())
                    {
                        this.Current = JsonConvert.DeserializeObject<T>(KustoBindingUtilities.SerializeRow(this._reader));
                        return true;
                    }
                }
                return false;
            }
        }
    }
}