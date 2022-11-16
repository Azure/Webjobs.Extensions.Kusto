// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Ingest;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common
{
    public class MockManagedStreamingClientFactory : IKustoClientFactory
    {
        private readonly IKustoIngestClient _ingestClient;
        public MockManagedStreamingClientFactory(IKustoIngestClient mockClient)
        {
            this._ingestClient = mockClient;
        }

        public IKustoIngestClient IngestClientFactory(string engineConnectionString)
        {
            return this._ingestClient;
        }
    }
}

