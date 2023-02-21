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
        // List of fields for usage tracking of Functions
        public const string KustoClientName = "Kusto.Function.Client";
        public static readonly string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static readonly string ClientDetailForTracing = $"{KustoClientName}:{AssemblyVersion}";
        public static readonly string ClientRequestId = $"AzFunctions.InputBinding;{AssemblyVersion}";
        public const string SDKClientName = "Kusto.Dotnet.Client";
        public const string SDKClientVersion = "11.2.2";
        public static readonly (string, string)[] AdditionalOptions = new[] { ("AppRuntime", System.Environment.Version.ToString()) };
    }
}
