// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common
{
    public class MockClientFactory : IKustoClientFactory
    {
        private readonly IKustoIngestClient _ingestClient;
        private readonly ICslQueryProvider _queryClient;
        public MockClientFactory(IKustoIngestClient mockClient)
        {
            this._ingestClient = mockClient;
        }

        public MockClientFactory(ICslQueryProvider queryClient)
        {
            this._queryClient = queryClient;
        }

        public IKustoIngestClient IngestClientFactory(string engineConnectionString, string managedIdentity, ILogger logger)
        {
            return this._ingestClient;
        }

        public ICslQueryProvider QueryProviderFactory(string engineConnectionString, string managedIdentity, ILogger logger)
        {
            return this._queryClient;
        }
    }
}

