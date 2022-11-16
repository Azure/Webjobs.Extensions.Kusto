// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    internal static class KustoConstants
    {
        public const string DefaultConnectionStringName = "KustoConnectionString";
        public const string IngestPrefix = "ingest-";
        public const string ProtocolSuffix = "://";
        public static readonly string ClientDetailForTracing = "Kusto.Function.Client:" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
