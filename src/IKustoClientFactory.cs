// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Ingest;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    internal interface IKustoClientFactory
    {
        IKustoIngestClient IngestClientFactory(string engineConnectionString);
    }
    internal class KustoManagedStreamingClientFactory : IKustoClientFactory
    {
        public IKustoIngestClient IngestClientFactory(string engineConnectionString)
        {
            var engineKcsb = new KustoConnectionStringBuilder(engineConnectionString)
            {
                ClientVersionForTracing = KustoConstants.ClientDetailForTracing
            };
            /*
                We expect minimal input from the user.The end user can just pass a connection string, we need to decipher the DM
                ingest endpoint as well from this. Both the engine and DM endpoint are needed for the managed ingest to happen
             */
            string dmConnectionStringEndpoint = engineKcsb.Hostname.Contains(KustoConstants.IngestPrefix) ? engineConnectionString : engineConnectionString.ReplaceFirstOccurrence(KustoConstants.ProtocolSuffix, KustoConstants.ProtocolSuffix + KustoConstants.IngestPrefix);
            var dmKcsb = new KustoConnectionStringBuilder(dmConnectionStringEndpoint)
            {
                ClientVersionForTracing = KustoConstants.ClientDetailForTracing
            };
            // Create a managed ingest connection
            return GetManagedStreamingClient(engineKcsb, dmKcsb);
        }
        private static IKustoIngestClient GetManagedStreamingClient(KustoConnectionStringBuilder engineKcsb, KustoConnectionStringBuilder dmKcsb)
        {
            return KustoIngestFactory.CreateManagedStreamingIngestClient(engineKcsb, dmKcsb);
        }
    }
}
