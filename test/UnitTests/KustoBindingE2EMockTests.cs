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

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.UnitTests
{
    public class KustoBindingE2EMockTests : IDisposable
    {
        private const string DatabaseName = "TestDatabase";
        private const string TableName = "TestTable";
        private const string QueryWithBoundParam = "declare query_parameters (name:string);TestTable | where Name == name";
        private const string QueryWithNoBoundParam = "TestTable | where Name == 'I4'";
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
        private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();

        public KustoBindingE2EMockTests()
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
            await this.RunTestAsync(typeof(KustoEndToEndFunctions), ingestClientFactory, nameof(KustoEndToEndFunctions.Outputs));
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
            string item1 = "I1";
            string item2 = "I2";
            string item3 = "I3";
            string item4 = "I4";
            string item5 = "I5";
            var setOfItems = new HashSet<string>
            {
                item1,item2,item5
            };
            var capturedCrpsWithParameters = new List<ClientRequestProperties>();
            var capturedCrpsWithoutParameters = new List<ClientRequestProperties>();
            // There are 5 calls made from input
            IDataReader mockDataReaderResult1 = KustoTestHelper.MockResultDataReaderItems(DatabaseName, item1, 1);
            IDataReader mockDataReaderResult2 = KustoTestHelper.MockResultDataReaderItems(DatabaseName, item2, 2);
            IDataReader mockDataReaderResult3 = KustoTestHelper.MockResultDataReaderItems(DatabaseName, item3, 3);
            IDataReader mockDataReaderResult4 = KustoTestHelper.MockResultDataReaderItems(DatabaseName, item4, 4);
            IDataReader mockDataReaderResult5 = KustoTestHelper.MockResultDataReaderItems(DatabaseName, item5, 5);
            mockQueryClient.SetupSequence(
                m => m.ExecuteQueryAsync(DatabaseName, QueryWithBoundParam, Capture.In(capturedCrpsWithParameters)))
            .ReturnsAsync(mockDataReaderResult1)
            .ReturnsAsync(mockDataReaderResult2)
            .ReturnsAsync(mockDataReaderResult5);
            // For the hardcoded query without parameters
            mockQueryClient
                .SetupSequence(m => m.ExecuteQueryAsync(DatabaseName, QueryWithNoBoundParam, Capture.In(capturedCrpsWithoutParameters)))
                .ReturnsAsync(mockDataReaderResult3)
                .ReturnsAsync(mockDataReaderResult4);
            // Act
            await this.RunTestAsync(typeof(KustoEndToEndFunctions), queryClientFactory, nameof(KustoEndToEndFunctions.Inputs));
            // Assert
            mockQueryClient.Verify(f => f.ExecuteQueryAsync(DatabaseName, QueryWithBoundParam, It.IsAny<ClientRequestProperties>()), Times.Exactly(3));
            // 2 times called without parameters
            mockQueryClient.Verify(f => f.ExecuteQueryAsync(DatabaseName, QueryWithNoBoundParam, It.IsAny<ClientRequestProperties>()), Times.Exactly(2));
            capturedCrpsWithParameters.ForEach(crp =>
            {
                // Check if the key exists
                Assert.Contains(crp.Parameters["name"], setOfItems);
                // Remove the keys
                setOfItems.Remove(crp.Parameters["name"]);
            });
            // This way validate all keys are present since matched keys get removed
            Assert.Empty(setOfItems);
            // No params passed. Should not have a name parameter in it
            capturedCrpsWithoutParameters.ForEach(crp =>
            {
                Assert.False(crp.Parameters.ContainsKey("name"));
            });
            mockQueryClient.VerifyAll();
        }

        [Theory]
        [InlineData(typeof(NoConnectionString), nameof(NoConnectionString.ErrBinding), typeof(ArgumentNullException))]
        [InlineData(typeof(NoCommandOrTable), nameof(NoCommandOrTable.ErrBinding), typeof(InvalidOperationException))]
        public async Task InvalidBindingAttributesError(Type testType, string testName, Type exceptionType)
        {
            //Arrange
            var mockQueryClient = new Mock<ICslQueryProvider>();
            var queryClientFactory = new MockClientFactory(mockQueryClient.Object);
            // Act
            FunctionIndexingException ex = await Assert.ThrowsAsync<FunctionIndexingException>(
                () => this.RunTestAsync(testType, queryClientFactory, testName));
            // Assert
            Assert.Equal(ex.InnerException.GetType(), exceptionType);
        }

        [Fact]
        public async Task EmptyDatabaseError()
        {
            //Arrange
            var mockQueryClient = new Mock<ICslQueryProvider>();
            var queryClientFactory = new MockClientFactory(mockQueryClient.Object);
            // Act
            FunctionIndexingException ex = await Assert.ThrowsAsync<FunctionIndexingException>(
                () => this.RunTestAsync(typeof(EmptyDatabase), queryClientFactory, nameof(EmptyDatabase.ErrBinding)));
            // Assert
            Assert.IsType<ArgumentNullException>(ex.InnerException);
        }

        [Fact]
        public async Task InvalidBindingTypeError()
        {
            //Arrange
            var mockQueryClient = new Mock<ICslQueryProvider>();
            var queryClientFactory = new MockClientFactory(mockQueryClient.Object);
            // Act
            FunctionIndexingException ex = await Assert.ThrowsAsync<FunctionIndexingException>(
                () => this.RunTestAsync(typeof(InvalidBinding), queryClientFactory, nameof(InvalidBinding.InvalidBindingType)));
            // Assert
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }

        [Fact]
        public async Task InvalidByteArrayEnumerableError()
        {
            //Arrange
            var mockQueryClient = new Mock<ICslQueryProvider>();
            var queryClientFactory = new MockClientFactory(mockQueryClient.Object);
            // Act
            FunctionIndexingException ex = await Assert.ThrowsAsync<FunctionIndexingException>(
                () => this.RunTestAsync(typeof(InvalidByteArrayEnumerable), queryClientFactory, nameof(InvalidByteArrayEnumerable.InvalidBindingType)));
            // Assert
        }

        [Fact]
        public async Task InvalidByteArrayCollectorError()
        {
            //Arrange
            var mockQueryClient = new Mock<ICslQueryProvider>();
            var queryClientFactory = new MockClientFactory(mockQueryClient.Object);
            // Act
            FunctionIndexingException ex = await Assert.ThrowsAsync<FunctionIndexingException>(
                () => this.RunTestAsync(typeof(InvalidByteArrayCollector), queryClientFactory, "InvalidCollector"));
            // Assert
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
            public static async Task Inputs(
                [Kusto(database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = "@name=I1", Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> itemOne,
                [Kusto(database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = "@name=I2", Connection = KustoConstants.DefaultConnectionStringName)] JArray itemTwo,
                [Kusto(database: DatabaseName, KqlCommand = QueryWithNoBoundParam, Connection = KustoConstants.DefaultConnectionStringName)] string itemThree,
                [Kusto(database: DatabaseName, KqlCommand = QueryWithNoBoundParam, Connection = KustoConstants.DefaultConnectionStringName)] IAsyncEnumerable<Item> itemFour,
                [Kusto(database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = "@name=I5", Connection = KustoConstants.DefaultConnectionStringName)] IAsyncEnumerable<Item> itemFive
            )
            {
                Assert.NotNull(itemOne);
                Assert.Equal(2, itemOne.Count());
                Assert.NotNull(itemTwo);
                Assert.Equal(2, itemTwo.Count);
                Assert.NotNull(itemThree);
                Assert.NotNull(itemFour);
                int numberOfItems = 0;
                await foreach (Item item in itemFour.ConfigureAwait(false))
                {
                    Assert.NotNull(item);
                    Assert.Equal(4, item.ID);
                    Assert.Equal("I4", item.Name);
                    numberOfItems++;
                }
                Assert.Equal(2, numberOfItems);
                // IAsync with params
                Assert.NotNull(itemFive);
                numberOfItems = 0;
                await foreach (Item item in itemFive.ConfigureAwait(false))
                {
                    Assert.NotNull(item);
                    Assert.Equal(5, item.ID);
                    Assert.Equal("I5", item.Name);
                    numberOfItems++;
                }
                Assert.Equal(2, numberOfItems);
            }
        }

        private class NoConnectionString
        {
            [NoAutomaticTrigger]
            public static void ErrBinding([Kusto(database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = "@name=I1")] IEnumerable<Item> _)
            {

            }
        }

        private class EmptyDatabase
        {
            [NoAutomaticTrigger]
            public static void ErrBinding([Kusto(database: "", KqlCommand = QueryWithBoundParam, KqlParameters = "@name=I1", Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> _)
            {

            }
        }

        private class NoCommandOrTable
        {
            [NoAutomaticTrigger]
            public static void ErrBinding([Kusto(database: DatabaseName, Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> _)
            {

            }
        }

        private class InvalidBinding
        {
            [NoAutomaticTrigger]
            public static void InvalidBindingType([Kusto(database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = "@name=I1", Connection = KustoConstants.DefaultConnectionStringName)] JObject _)
            {

            }
        }

        private class InvalidByteArrayEnumerable
        {
            [NoAutomaticTrigger]
            public static void InvalidBindingType([Kusto(database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = "@name=I1", Connection = KustoConstants.DefaultConnectionStringName)] byte[] _)
            {

            }
        }

        private class InvalidByteArrayCollector
        {
            [NoAutomaticTrigger]
            public static void InvalidCollector(
            [Kusto(database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] IAsyncCollector<byte[]> _)
            {
            }
        }
    }
}

