// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Kusto
{
    public sealed class KustoInputAttribute : OutputBindingAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KustoAttribute"/> class.
        /// </summary>
        /// <param name="database">The name of the database</param>
        public KustoInputAttribute(string database)
        {
            this.Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// The database name where the table resides into which data has to be written
        /// </summary>
        public string Database { get; private set; }

        /// <summary>
        /// The KQL query command that is used for input mapping. Refer samples for sample queries that use declare parameters
        /// </summary>
        public string KqlCommand { get; set; }

        /// <summary>
        /// Parameters that can be passed to the KQL query command above
        /// </summary>
        public string KqlParameters { get; set; }


        /// <summary>
        /// The name of the app setting where the Kusto connection string is stored
        /// Defaults to KustoConnectionString
        /// The attributes specified in the connection string are listed here
        /// https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto
        /// An example on the settings file could be the following : Using the AAD Credentials in the string , a connection will be attempted to the cluster Cluster and database DBName
        /// "KustoConnectionString": "Data Source=https://<Cluster>.kusto.windows.net;Database=<DBName>;Fed=True;AppClientId=<AppId>;AppKey=<AppKey>;Authority Id=<Tenant>"
        /// </summary>
        public string Connection { get; set; }

    }
}
