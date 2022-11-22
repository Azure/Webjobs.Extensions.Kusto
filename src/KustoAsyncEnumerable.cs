// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
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

            private async Task<bool> GetNextRowAsync()
            {
                if (this._kustoQueryContext.QueryProvider != null)
                {
                    if (this._reader == null)
                    {
                        string tracingRequestId = Guid.NewGuid().ToString();
                        ClientRequestProperties clientRequestProperties;
                        if (!string.IsNullOrEmpty(this._kustoQueryContext.ResolvedAttribute.KqlParameters))
                        {
                            // expect that this is a JSON in a specific format
                            // We expect that we have a declarative query mechanism to perform KQL
                            IDictionary<string, string> queryParameters = KustoBindingUtilities.ParseParameters(this._kustoQueryContext.ResolvedAttribute.KqlParameters);
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