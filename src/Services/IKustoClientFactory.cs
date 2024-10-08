﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Config;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.WebJobs.Extensions.Kusto.KustoConstants;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Wrap around Kusto internal classes and provides a mechanism to provide the query and ingest clients. Has an additional benefit that
    /// testing is a lot easier
    /// </summary>
    internal interface IKustoClientFactory
    {
        IKustoIngestClient IngestClientFactory(string engineConnectionString, string managedIdentity, string runtimeName, string ingestionType, ILogger logger);
        ICslQueryProvider QueryProviderFactory(string engineConnectionString, string managedIdentity, string runtimeName, ILogger logger);
        ICslAdminProvider AdminProviderFactory(string engineConnectionString, string managedIdentity, string runtimeName, ILogger logger);
    }
    internal class KustoClient : IKustoClientFactory
    {
        /// <summary>
        /// Given the engine connection string, return a kusto ingest client
        /// </summary>
        /// <param name="engineConnectionString">The engine connection string. The ingest URL will be derieved from this for the managed ingest</param>
        /// <param name="managedIdentity">MSI string to use Managed service identity</param>
        /// <param name="ingestionType">Ingestion type, managed or queued </param>
        /// <param name="logger">The logger to use to log the statements</param> 
        /// <returns>A managed ingest client. Attempts ingestion through streaming and then fallsback to Queued ingest mode</returns>
        public IKustoIngestClient IngestClientFactory(string engineConnectionString, string managedIdentity, string runtimeName, string ingestionType, ILogger logger)
        {
            KustoConnectionStringBuilder engineKcsb = GetKustoConnectionString(engineConnectionString, managedIdentity, runtimeName, OutputBindingType, logger);
            /*
                We expect minimal input from the user.The end user can just pass a connection string, we need to decipher the DM
                ingest endpoint as well from this. Both the engine and DM endpoint are needed for the managed ingest to happen
             */
            string dmConnectionStringEndpoint = engineKcsb.Hostname.Contains(IngestPrefix) ? engineConnectionString : engineConnectionString.ReplaceFirstOccurrence(ProtocolSuffix, ProtocolSuffix + IngestPrefix);
            KustoConnectionStringBuilder dmKcsb = GetKustoConnectionString(dmConnectionStringEndpoint, managedIdentity, runtimeName, OutputBindingType, logger);
            // Measure the time it takes for a connection
            var ingestClientInitialize = new Stopwatch();
            ingestClientInitialize.Start();
            // Create a managed ingest connection or a queued ingest
            IKustoIngestClient ingestClient = "queued".EqualsOrdinalIgnoreCase(ingestionType)
                ? GetQueuedIngestClient(dmKcsb)
                : GetManagedStreamingClient(engineKcsb, dmKcsb);
            ingestClientInitialize.Stop();
            logger.LogDebug($"Initializing ingest client with the connection string : {KustoBindingUtils.ToSecureString(engineConnectionString)}  took {ingestClientInitialize.ElapsedMilliseconds} milliseconds. IngestionType : {ingestionType}");
            return ingestClient;
        }
        private static IKustoIngestClient GetQueuedIngestClient(KustoConnectionStringBuilder dmKcsb)
        {
            return KustoIngestFactory.CreateQueuedIngestClient(dmKcsb, new QueueOptions { MaxRetries = 3 });
        }

        private static IKustoIngestClient GetManagedStreamingClient(KustoConnectionStringBuilder engineKcsb, KustoConnectionStringBuilder dmKcsb)
        {
            var fallbackPolicy = new ManagedStreamingIngestPolicy
            {
                ContinueWhenStreamingIngestionUnavailable = true
            };
            return KustoIngestFactory.CreateManagedStreamingIngestClient(engineKcsb, dmKcsb, ingestPolicy: fallbackPolicy);
        }

        /// <summary>
        /// Given the engine connection string, return a query client
        /// </summary>
        /// <param name="engineConnectionString">The engine connection string</param>
        /// <param name="managedIdentity">MSI string to use Managed service identity</param>
        /// <returns>A query client to execute KQL</returns>

        public ICslQueryProvider QueryProviderFactory(string engineConnectionString, string managedIdentity, string runtimeName, ILogger logger)
        {
            KustoConnectionStringBuilder engineKcsb = GetKustoConnectionString(engineConnectionString, managedIdentity, runtimeName, InputBindingType, logger);
            var timer = new Stopwatch();
            timer.Start();
            // Create a query client connection. This is needed in cases to debug any connection issues
            ICslQueryProvider queryProvider = KustoClientFactory.CreateCslQueryProvider(engineKcsb);
            timer.Stop();
            logger.LogDebug($"Initializing query client with the connection string : {KustoBindingUtils.ToSecureString(engineConnectionString)}  took {timer.ElapsedMilliseconds} milliseconds");
            return queryProvider;
        }

        /// <summary>
        /// Given the engine connection string, return an admin client
        /// </summary>
        /// <param name="engineConnectionString"></param>
        /// <param name="managedIdentity"></param>
        /// <param name="runtimeName"></param>
        /// <param name="logger"></param>
        /// <returns>An admin client to run admin commands</returns>
        public ICslAdminProvider AdminProviderFactory(string engineConnectionString, string managedIdentity, string runtimeName, ILogger logger)
        {
            KustoConnectionStringBuilder engineKcsb = GetKustoConnectionString(engineConnectionString, managedIdentity, runtimeName, InputBindingType, logger);
            var timer = new Stopwatch();
            timer.Start();
            // Create a query client connection. This is needed in cases to debug any connection issues
            ICslAdminProvider adminQueryProvider = KustoClientFactory.CreateCslAdminProvider(engineKcsb);
            timer.Stop();
            logger.LogDebug($"Initializing admin query client with the connection string : {KustoBindingUtils.ToSecureString(engineConnectionString)}  took {timer.ElapsedMilliseconds} milliseconds");
            return adminQueryProvider;
        }

        private static KustoConnectionStringBuilder GetKustoConnectionString(string connectionString, string managedIdentity, string runtimeName, string bindingDirection, ILogger logger)
        {
            KustoConnectionStringBuilder.DefaultPreventAccessToLocalSecretsViaKeywords = false;
            var kcsb = new KustoConnectionStringBuilder(connectionString)
            {
                ClientVersionForTracing = ClientDetailForTracing,
            };
            AdditionalOptions[FunctionsRuntime] = runtimeName;
            AdditionalOptions[BindingType] = bindingDirection;
            if (!string.IsNullOrEmpty(managedIdentity))
            {
                // There exists a managed identity. Check if that is UserManaged or System identity
                // use "system" to indicate the system-assigned identity
                if ("system".EqualsOrdinalIgnoreCase(managedIdentity))
                {
                    logger.LogDebug($"Using system managed user identity : {managedIdentity}");
                    AdditionalOptions[ManagedIdentity] = SystemManagedIdentity;
                    kcsb = kcsb.WithAadSystemManagedIdentity();
                }
                else
                {
                    logger.LogDebug($"Using user managed identity : {managedIdentity}");
                    AdditionalOptions[ManagedIdentity] = UserManagedIdentity;
                    kcsb = kcsb.WithAadUserManagedIdentity(managedIdentity);
                }
            }
            kcsb.SetConnectorDetails(name: AzFunctionsClientName, version: AssemblyVersion, additional: AdditionalOptions.Select(kv => (kv.Key, kv.Value)).ToArray(), sendUser: true);
            return kcsb;
        }
    }
}