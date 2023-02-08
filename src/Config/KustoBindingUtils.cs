// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Config
{
    internal class KustoBindingUtils
    {
        /// <summary>
        /// Given a KustoConnectionString, scrubs out sensitive parts of the string and returns a sanitized string
        /// </summary>
        /// <param name="KustoConnectionString"></param>
        /// <returns>A sanitized string that removes the sensisitive token from Kusto connection string</returns>
        internal static string ToSecureString(string KustoConnectionString)
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder
            {
                ConnectionString = KustoConnectionString
            };

            string[] securityQualifiers = new string[] {
                "AppKey", "Application Key", "ApplicationKey",
                "UserID", "User ID", "UID", "User",
                "User Token", "UsrToken", "UserToken", "UserToken",
                "Application Token", "AppToken", "ApplicationToken",
                "Authority Id", "TenantId", "Authority",
                "Application Client Id", "AppClientId", "ApplicationClientId"
            };
            foreach (string qualifier in securityQualifiers)
            {
                if (builder.ContainsKey(qualifier))
                {
                    builder[qualifier] = "*";
                }
            }
            return builder.ConnectionString;
        }
    }
}
