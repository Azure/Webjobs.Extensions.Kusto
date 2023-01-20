// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using static Microsoft.Azure.WebJobs.Extensions.Kusto.KustoQueryConverters;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto
{
    /// <summary>
    /// Exposes Kusto bindings.
    /// </summary>
    [Extension("Kusto")]
    internal class KustoExtensionConfigProvider : IExtensionConfigProvider
    {
        internal ConcurrentDictionary<string, IKustoIngestClient> IngestClientCache { get; } = new ConcurrentDictionary<string, IKustoIngestClient>();
        internal ConcurrentDictionary<string, ICslQueryProvider> QueryClientCache { get; } = new ConcurrentDictionary<string, ICslQueryProvider>();
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IKustoClientFactory _kustoClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="KustoBindingConfigProvider/>"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either parameter is null.
        /// </exception>
        public KustoExtensionConfigProvider(IConfiguration configuration, ILoggerFactory loggerFactory, IKustoClientFactory kustoClientFactory)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this._kustoClientFactory = kustoClientFactory ?? throw new ArgumentNullException(nameof(kustoClientFactory));
        }

        /// <summary>
        /// Initializes the Kusto binding rules.
        /// </summary>
        /// <param name="context"> The config context.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if context is null.
        /// </exception>
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            ILogger logger = this._loggerFactory.CreateLogger(LogCategories.Bindings);
#pragma warning disable CS0618 // Cannot use var. FluentBindingRule is in Beta
            FluentBindingRule<KustoAttribute> inputOutputRule = context.AddBindingRule<KustoAttribute>();
            // Validate the attributes we have
            inputOutputRule.AddValidator(this.ValidateConnection);
            // Bind to the types
            inputOutputRule.BindToCollector<KustoOpenType>(typeof(KustoAsyncCollectorBuilder<>), logger, this);
            var converter = new KustoCslQueryConverter(this);
            inputOutputRule.BindToInput(converter);
            inputOutputRule.BindToInput<string>(typeof(KustoGenericsConverter<string>), logger, this);
            inputOutputRule.BindToInput<JArray>(typeof(KustoGenericsConverter<JArray>), logger, this);
            inputOutputRule.BindToInput<OpenType>(typeof(KustoGenericsConverter<>), logger, this);
        }
        internal void ValidateConnection(KustoAttribute attribute, Type paramType)
        {
            if (string.IsNullOrEmpty(attribute.Connection))
            {
                string attributeProperty = $"{nameof(KustoAttribute)}.{nameof(KustoAttribute.Connection)}";
                throw new ArgumentNullException(attributeProperty, $"Parameter {attributeProperty} should be passed as an environment variable. This value resolved to null");
            }

            // Empty database check is added right when the KustoAttribute is constructed. This however is deferred here. 
            // TODO : Add check based on parameters and parameter indexes ?
            if (string.IsNullOrEmpty(attribute.TableName) && string.IsNullOrEmpty(attribute.KqlCommand))
            {
                string attributeProperty = $"{nameof(KustoAttribute)}.{nameof(KustoAttribute.TableName)} or {nameof(KustoAttribute)}.{nameof(KustoAttribute.KqlCommand)}";
                throw new InvalidOperationException(
                    $"The {attributeProperty} property cannot be an empty value.");
            }
        }

        internal KustoIngestContext CreateIngestionContext(KustoAttribute kustoAttribute)
        {
            IKustoIngestClient service = this.GetIngestClient(kustoAttribute);
            return new KustoIngestContext
            {
                IngestService = service,
                ResolvedAttribute = kustoAttribute,
            };
        }

        internal IKustoIngestClient GetIngestClient(KustoAttribute kustoAttribute)
        {
            string connection = string.IsNullOrEmpty(kustoAttribute.Connection) ? KustoConstants.DefaultConnectionStringName : kustoAttribute.Connection;
            string engineConnectionString = this.GetConnectionString(connection);
            if (string.IsNullOrEmpty(engineConnectionString))
            {
                throw new ArgumentNullException(engineConnectionString, $"Parameter {kustoAttribute.Connection} should be passed as an environment variable. This value resolved to null");
            }
            string cacheKey = BuildCacheKey(engineConnectionString);
            return this.IngestClientCache.GetOrAdd(cacheKey, (c) => this._kustoClientFactory.IngestClientFactory(engineConnectionString));
        }


        internal KustoQueryContext CreateQueryContext(KustoAttribute kustoAttribute)
        {
            ICslQueryProvider queryProvider = this.GetQueryClient(kustoAttribute);
            return new KustoQueryContext
            {
                QueryProvider = queryProvider,
                ResolvedAttribute = kustoAttribute,
            };
        }

        internal ICslQueryProvider GetQueryClient(KustoAttribute kustoAttribute)
        {
            string connection = string.IsNullOrEmpty(kustoAttribute.Connection) ? KustoConstants.DefaultConnectionStringName : kustoAttribute.Connection;
            string engineConnectionString = this.GetConnectionString(connection);
            if (string.IsNullOrEmpty(engineConnectionString))
            {
                throw new ArgumentNullException(engineConnectionString, $"Parameter {kustoAttribute.Connection} should be passed as an environment variable. This value resolved to null");
            }
            string cacheKey = BuildCacheKey(engineConnectionString);
            return this.QueryClientCache.GetOrAdd(cacheKey, (c) => this._kustoClientFactory.QueryProviderFactory(engineConnectionString));
        }

        internal string GetConnectionString(string connectionStringSetting)
        {
            // Already validated upfront in Validate that ConnectionString setting is passed in and not null
            return this._configuration.GetConnectionStringOrSetting(connectionStringSetting);
        }
        internal static string BuildCacheKey(string connectionString)
        {
            return $"C-{connectionString.GetHashCode()}";
        }
    }
    /// <summary>
    /// Wrapper around OpenType to receive data correctly from output bindings (not as byte[])
    /// This can be used for general "T --> JObject" bindings. 
    /// The exact definition here comes from the WebJobs v1.0 Queue binding.
    /// refer https://github.com/Azure/azure-webjobs-sdk/blob/dev/src/Microsoft.Azure.WebJobs.Host/Bindings/OpenType.cs#L390.
    /// </summary>
    internal class KustoOpenType : OpenType.Poco
    {
        // return true when type is an "System.Object" to enable Object binding.
        public override bool IsMatch(Type type, OpenTypeMatchContext context)
        {
            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return false;
            }

            if (type.FullName == "System.Object")
            {
                return true;
            }
            return base.IsMatch(type, context);
        }
    }
}