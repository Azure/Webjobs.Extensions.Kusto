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
        /// <param name="database">The name of the database</param>
        public KustoAttribute(string database)
        {
            this.Database = AssignValue(database, nameof(database));
        }

        [AutoResolve]
        public string Database { get; private set; }

        [AutoResolve]
        public string TableName { get; set; }

        [AutoResolve]
        public string MappingRef { get; set; }

        [AutoResolve]
        public string DataFormat { get; set; }

        [AutoResolve]
        public string KqlCommand { get; set; }

        [AutoResolve]
        public string KqlParameters { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private string DebuggerDisplay
        {
            get
            {
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}(Mapping={1}, DataFormat={2})",
                        this.TableName, this.MappingRef, this.DataFormat);
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