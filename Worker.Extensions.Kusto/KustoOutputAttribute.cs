// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Kusto
{
    public sealed class KustoOutputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KustoAttribute"/> class.
        /// </summary>
        /// <param name="Database">The name of the Database</param>
        public KustoOutputAttribute(string Database)
        {
            this.Database = Database ?? throw new ArgumentNullException(nameof(Database));
        }

        /// <summary>
        /// The Database name where the table resides into which data has to be written
        /// </summary>
        public string Database { get; private set; }

        /// <summary>
        /// The table to which data has to be written
        /// </summary>

        public string TableName { get; set; }

        /// <summary>
        /// The mapping reference that is used for data ingestion
        /// </summary>

        public string MappingRef { get; set; }

        /// <summary>
        /// The data format for ingestion. Currently CSV and JSON are supported
        /// </summary>

        public string DataFormat { get; set; }

        /// <summary>
        /// The name of the app setting where the Kusto connection string is stored
        /// Defaults to KustoConnectionString
        /// The attributes specified in the connection string are listed here
        /// https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto
        /// An example on the settings file could be the following : Using the AAD Credentials in the string , a connection will be attempted to the cluster Cluster and Database DBName
        /// "KustoConnectionString": "Data Source=https://<Cluster>.kusto.windows.net;Database=<DBName>;Fed=True;AppClientId=<AppId>;AppKey=<AppKey>;Authority Id=<Tenant>"
        /// </summary>
        public string Connection { get; set; }

        /// <summary>
        /// An option to set the ManagedServiceIdentity option. If set to "system" will use SystemManagedIdentity else use UserManagedIdentity
        /// </summary>
        public string ManagedServiceIdentity { get; set; }

    }
}