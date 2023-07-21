// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Kusto
{
    /// <summary>
    /// Setup an 'output' binding to an Kusto.
    /// - Establish a connection to a Kusto and insert rows into a given table, in the case of an output binding
    /// - WIP - Input binding
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [Binding]
    public sealed class KustoAttribute : Attribute, IConnectionProvider
    {
        /// <summary>Initializes a new instance of the <see cref="KustoAttribute"/> class.</summary>
        /// <param name="Database">The name of the Database</param>
        public KustoAttribute(string Database)
        {
            this.Database = AssignValue(Database, nameof(Database));
        }

        /// <summary>
        /// An option to set the ManagedServiceIdentity option. If set to "system" will use SystemManagedIdentity else use UserManagedIdentity
        /// </summary>
        [AutoResolve]
        public string ManagedServiceIdentity { get; set; }

        /// <summary>
        /// The database name to use
        /// </summary>
        [AutoResolve]
        public string Database { get; private set; }

        /// <summary>
        /// In case of Output binding, the name of the table to use
        /// </summary>
        [AutoResolve]
        public string TableName { get; set; }

        /// <summary>
        /// References an existing mapping created that can be used during ingestion
        /// </summary>
        [AutoResolve]
        public string MappingRef { get; set; }

        /// <summary>
        /// The dataformat to use JSON and CSV are currently supported
        /// </summary>
        [AutoResolve]
        public string DataFormat { get; set; }

        /// <summary>
        /// In case of Input binding, the KqlCommand a.k.a KQL to execute
        /// </summary>
        [AutoResolve]
        public string KqlCommand { get; set; }

        /// <summary>
        /// The parameter to use in the KqlCommand (@param1=value1,@param2=value2)
        /// </summary>
        [AutoResolve]
        public string KqlParameters { get; set; }

        /// <summary>
        /// The parameter to use in the ClientRequestProperties in the form (@param1=value1,@param2=value2). 
        /// Refer https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/netfx/request-properties#clientrequestproperties for details on properties
        /// </summary>
        [AutoResolve]
        public string ClientRequestProperties { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private string DebuggerDisplay
        {
            get
            {
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}(MappingRef={1}, DataFormat={2}, MSI={3}, KqlCommand={4}, KqlParameters={5})",
                        this.TableName, this.MappingRef, this.DataFormat, this.ManagedServiceIdentity, this.KqlCommand, this.KqlParameters);
                }
            }
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Kusto connection string. Ref : https://learn.microsoft.com/en-us/azure/data-explorer/kusto/api/connection-strings/kusto.
        /// </summary>
        public string Connection { get; set; }

        private static string AssignValue(string value, string keyName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(keyName);
            }
            else
            {
                return value;
            }
        }
    }
}