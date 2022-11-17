// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
/*   
using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    internal class KustoConverters
    {
        internal class KustoConverter : IConverter<KustoAttribute, SqlCommand>
        {
            private readonly IConfiguration _configuration;
            private readonly ILogger _logger;
            /// <summary>
            /// Initializes a new instance of the <see cref="KustoConverter/>"/> class.
            /// </summary>
            /// <param name="configuration"></param>
            /// <param name="logger">ILogger used to log information and warnings</param>
            /// <exception cref="ArgumentNullException">
            /// Thrown if the configuration is null
            /// </exception>
            public KustoConverter(IConfiguration configuration, ILogger logger)
            {
                this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                this._logger = logger;
            }

            /// <summary>
            /// </summary>
            /// <param name="attribute">
            /// Contains the KQL , Database and Connection to query the data from
            /// </param>
            /// <returns>The SqlCommand</returns>
         public SqlCommand Convert(KustoAttribute attribute)
            {
                this._logger.LogDebug("BEGIN Convert (SqlCommand)");
                var sw = Stopwatch.StartNew();
                try
                {
                    SqlCommand command = SqlBindingUtilities.BuildCommand(attribute, SqlBindingUtilities.BuildConnection(
                                       attribute.ConnectionStringSetting, this._configuration));
                    this._logger.LogDebugWithThreadId($"END Convert (SqlCommand) Duration={sw.ElapsedMilliseconds}ms");
                    return command;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

        }
    }
}*/
