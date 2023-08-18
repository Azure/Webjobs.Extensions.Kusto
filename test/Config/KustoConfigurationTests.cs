// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Config
{
    public class KustoConfigurationTests : IDisposable
    {
        private static readonly IConfiguration _baseConfig = KustoTestHelper.BuildConfiguration();
        private readonly ILogger _logger = new LoggerFactory().CreateLogger<KustoConfigurationTests>();

        [Fact]
        public void ConfigurationCachesClients()
        {
            // Given
            KustoExtensionConfigProvider kustoExtensionConfigProvider = InitializeCreatesClients();
            var attribute = new KustoAttribute("unittestdb")
            {
                TableName = "Items"
            };
            // When
            _ = kustoExtensionConfigProvider.CreateIngestionContext(attribute);
            _ = kustoExtensionConfigProvider.CreateIngestionContext(attribute);
            var asyncBuilder = new KustoAsyncCollectorBuilder<KustoOpenType>(this._logger, kustoExtensionConfigProvider);
            // Then
            Assert.NotNull(asyncBuilder);
            Assert.Single(kustoExtensionConfigProvider.IngestClientCache);
            // Given
            var queryAttribute = new KustoAttribute("unittestdb")
            {
                TableName = "Items",
                KqlCommand = "Storms | take 10"
            };
            // When
            _ = kustoExtensionConfigProvider.CreateQueryContext(queryAttribute);
            _ = kustoExtensionConfigProvider.CreateQueryContext(queryAttribute);
            var asyncQueryClientBuilder = new KustoAsyncCollectorBuilder<KustoOpenType>(this._logger, kustoExtensionConfigProvider);
            // Then
            Assert.NotNull(asyncQueryClientBuilder);
            Assert.Single(kustoExtensionConfigProvider.QueryClientCache);
        }
        [Fact]
        public void FailsWhenConnectionStringIsNotResolvable()
        {
            // Given
            KustoExtensionConfigProvider kustoExtensionConfigProvider = InitializeCreatesClients();
            var invalidAttribute = new KustoAttribute("unittestdb")
            {
                TableName = "Items",
                Connection = "InvalidConnectionString"
            };
            Exception functionIndexingException = Record.Exception(() => kustoExtensionConfigProvider.ValidateConnection(invalidAttribute, typeof(KustoAttribute)));
            string parameterNullException = functionIndexingException.GetBaseException().Message;
            Assert.Equal("Parameter KustoAttribute.Connection should be passed as an environment variable. This value resolved to null", parameterNullException);
        }

        private static KustoExtensionConfigProvider InitializeCreatesClients()
        {
            var nameResolver = new KustoNameResolver();
            var mockIngestionClient = new Mock<IKustoIngestClient>(MockBehavior.Strict);
            var ingestClientFactory = new MockClientFactory(mockIngestionClient.Object);

            var kustoExtensionConfigProvider = new KustoExtensionConfigProvider(_baseConfig, NullLoggerFactory.Instance, ingestClientFactory);
            kustoExtensionConfigProvider.IngestClientCache.Clear();
            kustoExtensionConfigProvider.QueryClientCache.Clear();
            ExtensionConfigContext context = KustoTestHelper.CreateExtensionConfigContext(nameResolver);
            // Should Initialize and register the validators and should not throw an error
            kustoExtensionConfigProvider.Initialize(context);
            // Then
            Assert.NotNull(kustoExtensionConfigProvider);
            return kustoExtensionConfigProvider;
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}