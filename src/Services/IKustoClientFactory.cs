// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Kusto.Ingest;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Wrap around Kusto internal classes and provides a mechanism to provide the query and ingest clients. Has an additional benefit that
    /// testing is a lot easier
    /// </summary>
    internal interface IKustoClientFactory
    {
        IKustoIngestClient IngestClientFactory(string engineConnectionString);
        ICslQueryProvider QueryProviderFactory(string engineConnectionString);
    }
    internal class KustoClient : IKustoClientFactory
    {
        /// <summary>
        /// Given the engine connection string, return a managed ingest client
        /// </summary>
        /// <param name="engineConnectionString">The engine connection string. The ingest URL will be derieved from this for the managed ingest</param>
        /// <returns>A managed ingest client. Attempts ingestion through streaming and then fallsback to Queued ingest mode</returns>
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

            if (!string.IsNullOrEmpty(engineKcsb?.EmbeddedManagedIdentity))
            {
                string msi = engineKcsb.EmbeddedManagedIdentity;
                // There exists a managed identity. Check if that is UserManaged or System identity
                // Ref doc https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto:
                // use "system" to indicate the system-assigned identity
                if ("system".EqualsOrdinalIgnoreCase(msi))
                {
                    engineKcsb.WithAadSystemManagedIdentity();
                    dmKcsb.WithAadSystemManagedIdentity();
                }
                else
                {
                    engineKcsb.WithAadUserManagedIdentity(msi);
                    dmKcsb.WithAadUserManagedIdentity(msi);

                }
            }



            // Create a managed ingest connection
            return GetManagedStreamingClient(engineKcsb, dmKcsb);
        }
        private static IKustoIngestClient GetManagedStreamingClient(KustoConnectionStringBuilder engineKcsb, KustoConnectionStringBuilder dmKcsb)
        {
            return KustoIngestFactory.CreateManagedStreamingIngestClient(engineKcsb, dmKcsb);
        }

        /// <summary>
        /// Given the engine connection string, return a query client
        /// </summary>
        /// <param name="engineConnectionString">The engine connection string</param>
        /// <returns>A query client to execute KQL</returns>

        public ICslQueryProvider QueryProviderFactory(string engineConnectionString)
        {
            var engineKcsb = new KustoConnectionStringBuilder(engineConnectionString)
            {
                ClientVersionForTracing = KustoConstants.ClientDetailForTracing
            };
            return KustoClientFactory.CreateCslQueryProvider(engineKcsb);
        }
    }
}
