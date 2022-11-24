// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Indexers;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests
{
    public class KustoBindingE2ETests : IDisposable
    {
        private const string DatabaseName = "TestDatabase";
        private const string TableName = "TestTable";
        private const string Query = "declare query_parameters (name:string);TestTable | where Name == name";
        //private static readonly IConfiguration _baseConfig = KustoTestHelper.BuildConfiguration();
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
        private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();

        public KustoBindingE2ETests()
        {
            this._loggerFactory.AddProvider(this._loggerProvider);
        }

        [Fact]
        public async Task OutputBindings()
        {
            // Arrange
            var mockIngestionClient = new Mock<IKustoIngestClient>();
            var mockIngestionResult = new Mock<IKustoIngestionResult>();
            var ingestionStatus = new IngestionStatus()
            {
                Status = Status.Succeeded,
            };
            var actualIngestDataStreams = new List<Stream>();
            var actualKustoIngestionProps = new List<KustoIngestionProperties>();
            var actualStreamSourceOptions = new List<StreamSourceOptions>();
            // Ingestion results
            mockIngestionResult.Setup(m => m.GetIngestionStatusCollection()).Returns(Enumerable.Repeat(ingestionStatus, 1));
            mockIngestionResult.Setup(m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>())).Returns(ingestionStatus);
            // set the ingestion behavior
            mockIngestionClient.Setup(m => m.IngestFromStreamAsync(
                It.IsAny<Stream>(),
                It.IsAny<KustoIngestionProperties>(),
                It.IsAny<StreamSourceOptions>())).
                ReturnsAsync(mockIngestionResult.Object).
                Callback<Stream, KustoIngestionProperties, StreamSourceOptions>((s, kip, sso) =>
                {
                    Validate(s, kip, sso);
                });
            var ingestClientFactory = new MockClientFactory(mockIngestionClient.Object);
            // Act
            await this.RunTestAsync(typeof(KustoEndToEndFunctions), ingestClientFactory, "Outputs");
            // Assert
            mockIngestionClient.Verify(m => m.IngestFromStreamAsync(
                            It.IsAny<Stream>(),
                            It.IsAny<KustoIngestionProperties>(),
                            It.IsAny<StreamSourceOptions>()), Times.Exactly(5));
            mockIngestionResult.Verify(m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>()), Times.Exactly(5));
            mockIngestionClient.VerifyAll();
        }

        private static void Validate(Stream actualStreamData, KustoIngestionProperties actualKustoIngestionProperties, StreamSourceOptions actualOptions)
        {
            Assert.NotNull(actualStreamData);
            Assert.NotNull(actualOptions);
            Assert.Equal(TableName, actualKustoIngestionProperties.TableName);
            Assert.Equal(DatabaseName, actualKustoIngestionProperties.DatabaseName);
            List<Item> items = KustoTestHelper.LoadItems(actualStreamData);
            Assert.NotNull(items);
            if (items.Count == 1)
            {
                Assert.Equal("json", actualKustoIngestionProperties.Format.ToString());
            }
            else
            {
                Assert.Equal("multijson", actualKustoIngestionProperties.Format.ToString());
            }
        }


        [Fact]
        public async Task InputBindings()
        {
            //Arrange
            var mockQueryClient = new Mock<ICslQueryProvider>();
            var queryClientFactory = new MockClientFactory(mockQueryClient.Object);

            string itemName = "I1";
            IDataReader mockDataReader = KustoTestHelper.MockResultDataReaderItems(itemName, 1);
            mockQueryClient.Setup(
                m => m.ExecuteQueryAsync(DatabaseName, Query, It.IsAny<ClientRequestProperties>()))
            .Returns(Task.FromResult(mockDataReader))
            .Callback<string, string, ClientRequestProperties>((db, query, crp) =>
            {
                Assert.True(crp.HasParameter("name"));
            });
            // Act
            await this.RunTestAsync(typeof(KustoEndToEndFunctions), queryClientFactory, "Inputs");
            // Assert
            mockQueryClient.Verify(f => f.ExecuteQueryAsync(DatabaseName, It.IsAny<string>(), It.IsAny<ClientRequestProperties>()), Times.Once());
            mockQueryClient.VerifyAll();
        }

        [Fact]
        public async Task NoConnectionStringError()
        {
            //Arrange
            var mockQueryClient = new Mock<ICslQueryProvider>();
            var queryClientFactory = new MockClientFactory(mockQueryClient.Object);
            // Act
            FunctionIndexingException ex = await Assert.ThrowsAsync<FunctionIndexingException>(
                () => this.RunTestAsync(typeof(KustoEndToEndFunctions), queryClientFactory, "NoConnectionString"));
            // Assert
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Fact]
        public async Task InvalidBindingTypeError()
        {
            //Arrange
            var mockQueryClient = new Mock<ICslQueryProvider>();
            var queryClientFactory = new MockClientFactory(mockQueryClient.Object);
            // Act
            FunctionInvocationException ex = await Assert.ThrowsAsync<FunctionInvocationException>(
                () => this.RunTestAsync(typeof(KustoEndToEndFunctions), queryClientFactory, "InvalidBindingType"));
            // Assert
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }


        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._loggerFactory != null)
                {
                    this._loggerFactory.Dispose();
                }
            }
        }

        private async Task RunTestAsync(Type testType, IKustoClientFactory kustoClientFactory, string testName, object argument = null, bool includeDefaultConnectionString = true)
        {
            var locator = new ExplicitTypeLocator(testType);
            var arguments = new Dictionary<string, object>
            {
                { "triggerData", argument }
            };
            IHost host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    builder.AddBuiltInBindings();
                    builder.AddKusto();
                })
                .ConfigureAppConfiguration(c =>
                {
                    c.Sources.Clear();
                    if (includeDefaultConnectionString)
                    {
                        c.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { ConnectionStringNames.Storage, "UseDevelopmentStorage=true" },
                            { KustoConstants.DefaultConnectionStringName, KustoTestHelper.DefaultTestConnectionString }
                        });
                    }
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITypeLocator>(locator);
                    services.AddSingleton(kustoClientFactory);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(this._loggerProvider);
                })
                .Build();

            await host.StartAsync();
            await host.GetJobHost().CallAsync(testType.GetMethod(testName), arguments);
            await host.StopAsync();
        }
        private class KustoEndToEndFunctions
        {
            [NoAutomaticTrigger]
            public static void Outputs(
                [Kusto(database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] out object newItem,
                [Kusto(database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] out string newItemString,
                [Kusto(database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] out object[] arrayItem,
                [Kusto(database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] IAsyncCollector<object> asyncCollector,
                [Kusto(database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] ICollector<object> collector)
            {
                newItem = new { };
                newItemString = "{}";
                arrayItem = new Item[]
                {
                    new Item(),
                    new Item()
                };
                Task.WaitAll(new[]
                {
                    asyncCollector.AddAsync(new { }),
                    asyncCollector.AddAsync(new { })
                });
                collector.Add(new { });
                collector.Add(new { });
            }
            [NoAutomaticTrigger]
            public static void Inputs(
            [Kusto(database: DatabaseName, KqlCommand = Query, KqlParameters = "@name=I1", Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> itemOne,
            [Kusto(database: DatabaseName, KqlCommand = Query, KqlParameters = "@name=I2", Connection = KustoConstants.DefaultConnectionStringName)] IAsyncEnumerable<Item> itemTwo,
            [Kusto(database: DatabaseName, KqlCommand = Query, KqlParameters = "@name=I3", Connection = KustoConstants.DefaultConnectionStringName)] JArray itemThree
            )
            {
                Assert.NotNull(itemOne);
                Assert.Equal(2, itemOne.Count());
                Assert.NotNull(itemTwo);
                Assert.NotNull(itemThree);
                Assert.Equal(2, itemThree.Count);
            }
            public static void NoConnectionString(
            [Kusto(database: DatabaseName, KqlCommand = Query, KqlParameters = "@name=I1", Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> _)
            {

            }
            public static void InvalidBindingType(
            [Kusto(database: DatabaseName, KqlCommand = Query, KqlParameters = "@name=I1", Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<JObject> _)
            {

            }
        }
    }
}
