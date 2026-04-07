// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    internal static class KustoConstants
    {
        public const string DefaultConnectionStringName = "KustoConnectionString";
        public const string IngestPrefix = "ingest-";
        public const string ProtocolSuffix = "://";
        // List of fields for usage tracking of Functions
        public const string AzFunctionsClientName = "AzFunctions.Client";
        public const string FunctionsRuntime = "FunctionsRuntime";
        public const string BindingType = "BindingType";
        public const string ManagedIdentity = "MSI";
        public const string SystemManagedIdentity = "SystemManagedIdentity";
        public const string UserManagedIdentity = "UserManagedIdentity";
        public const string InputBindingType = "InputBinding";
        public const string OutputBindingType = "OutputBinding";
        public const string FunctionsRuntimeHostKey = "FUNCTIONS_WORKER_RUNTIME";
        // Diagnostic event keys used by the Functions runtime to surface events in the Portal
        public const string DiagnosticEventKey = "MS_DiagnosticEvent";
        public const string HelpLinkKey = "MS_HelpLink";
        public const string ErrorCodeKey = "MS_ErrorCode";
        // Kusto extension error codes
        public const string ConnectionErrorCode = "AZFK0001";
        public const string IngestionErrorCode = "AZFK0002";
        public const string QueryErrorCode = "AZFK0003";
        // Help links for documentation
        public const string KustoBindingHelpLink = "https://learn.microsoft.com/azure/azure-functions/functions-bindings-azure-data-explorer";
        public static readonly string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static readonly string ClientDetailForTracing = $"{AzFunctionsClientName}:{AssemblyVersion}";
        public static readonly string ClientRequestId = $"AzFunctions.InputBinding;{AssemblyVersion}";
        public static readonly Dictionary<string, string> AdditionalOptions = new Dictionary<string, string>()
        {
            { "OSVersion", GetOsVersion() }
        };

        private static string GetOsVersion()
        {
            string osVersion;
            try
            {
                osVersion = Environment.OSVersion?.Platform.ToString();
            }
            catch (Exception)
            {
                osVersion = "N/A";
            }
            return osVersion;
        }
    }
}