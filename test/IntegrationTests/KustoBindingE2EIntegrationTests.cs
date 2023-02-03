// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.IntegrationTests
{
    // The EndToEnd tests require the KustoConnectionString environment variable to be set.
    [Trait("Category", "E2E")]
    public class KustoBindingE2EIntegrationTests : BeforeAfterTestAttribute, IDisposable
    {
        // These have to be decared as consts for the Bindings attributes to use
        private const string TableName = "kusto_functions_e2e_tests";
        // Create the table
        private readonly string CreateItemTable = $".create-merge table {TableName}(ID:int,Name:string, Cost:double,Timestamp:datetime)";
        private readonly string ClearItemTable = $".clear table {TableName} data";
        private readonly string DropTable = $".drop table {TableName}";
        // Queries for input binding with parameters
        private const string QueryWithBoundParam = "declare query_parameters(startId:int,endId:int);kusto_functions_e2e_tests | where ID >= startId and ID <= endId and ingestion_time()>ago(10s)";
        // Queries for input binding without parameters
        private const string QueryWithNoBoundParam = "kusto_functions_e2e_tests| where ingestion_time() > ago(10s) | order by ID asc";
        // Make sure that the InitialCatalog parameter in the tests has the same value as the Database name
        private const string DatabaseName = "sdktestsdb";
        private const int startId = 1;
        // Query parameter to get a single row where start and end are the same
        private const string KqlParameterSingleItem = "@startId=1,@endId=1";
        private const string KqlParameter2ValuesInArray = "@startId=6,@endId=7";
        private const string KqlParameterMSIItem = "@startId=1000,@endId=1000";
        // A client to perform all the assertions
        protected ICslQueryProvider KustoQueryClient { get; private set; }
        protected ICslAdminProvider KustoAdminClient { get; private set; }
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
        private readonly TestLoggerProvider _loggerProvider = new TestLoggerProvider();

        [Fact]
        public async Task KustoFunctionsE2E()
        {
            ILogger logger = this._loggerFactory.CreateLogger<KustoBindingE2EIntegrationTests>();
            IHost jobHost = await this.StartHostAsync(typeof(KustoEndToEndTestClass));
            IConfiguration hostConfiguration = jobHost.Services.GetRequiredService<IConfiguration>();
            // The environment variable 'KustoConnectionString' has to be defined for the E2E Tests to work
            string kustoConnectionString = hostConfiguration.GetConnectionStringOrSetting(KustoConstants.DefaultConnectionStringName).ToString();
            if (string.IsNullOrEmpty(kustoConnectionString))
            {
                throw new ArgumentException("KustoConnectionString environment variable must be set for the E2E tests to run");
            }
            var engineKcsb = new KustoConnectionStringBuilder(kustoConnectionString);
            this.KustoQueryClient = KustoClientFactory.CreateCslQueryProvider(engineKcsb);
            this.KustoAdminClient = KustoClientFactory.CreateCslAdminProvider(engineKcsb);
            // Create the table for the tests
            System.Data.IDataReader tableCreationResult = this.KustoAdminClient.ExecuteControlCommand(DatabaseName, this.CreateItemTable);
            // Since this is a merge , if there is another table get it cleared for tests
            this.KustoAdminClient.ExecuteControlCommand(this.ClearItemTable);
            Assert.NotNull(tableCreationResult);
            var parameter = new Dictionary<string, object>
            {
                ["id"] = startId
            };
            // Output binding tests
            await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.Outputs), parameter);
            // Validate all rows written in output bindings can be queries
            await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.Inputs), parameter);
            // Fail scenario
            try
            {
                await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.InputFail), parameter);
            }
            catch (Exception ex)
            {
                // TODO validate this error
                Assert.IsType<FunctionInvocationException>(ex);
            }

            try
            {
                await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.OutputFail), parameter);
            }
            catch (Exception ex)
            {
                Assert.IsType<FunctionInvocationException>(ex);
            }
            // Tests for managed service identity
            string tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            string appId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            string appSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
            {
                logger.LogWarning("Environment variables AZURE_TENANT_ID/AZURE_CLIENT_ID/AZURE_CLIENT_SECRET are not set. MSI tests will not be run");
            }
            else
            {
                try
                {
                    await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.OutputMSI), parameter);
                    await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.InputMSI), parameter);
                }
                catch (Exception ex)
                {
                    logger.LogError("Exception executing MSI tests", ex);
                    Assert.Fail(ex.Message);
                }

            }
        }

        /*
         Add the KustoBindings to the startup host
         */
        private async Task<IHost> StartHostAsync(Type testType)
        {
            var locator = new ExplicitTypeLocator(testType);

            IHost host = new HostBuilder().UseEnvironment("Development")
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(this._loggerProvider);
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .ConfigureWebJobs(builder =>
                {
                    builder.Services.AddSingleton<IKustoClientFactory>(new KustoClient());
                    builder.AddKusto();
                })
                .ConfigureAppConfiguration(c =>
                {
                    c.AddTestSettings();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITypeLocator>(locator);
                })
                .Build();

            await host.StartAsync();
            return host;
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
#pragma warning disable xUnit1013
        public override void After(MethodInfo methodUnderTest)
        {
            // Drop the tables once done
            _ = this.KustoAdminClient.ExecuteControlCommand(DatabaseName, this.DropTable);
            this.KustoAdminClient.Dispose();
            this.KustoQueryClient.Dispose();
        }
#pragma warning restore xUnit1013

        private class KustoEndToEndTestClass
        {
            [NoAutomaticTrigger]
            public static void Outputs(
                int id,
                [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] out object newItem,
                [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] out string newItemString,
                [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] out object[] arrayItem,
                [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] IAsyncCollector<object> asyncCollector)
            {
                /*
                 Add an individual item-1
                 */
                newItem = GetItem(id);
                /*
                 Individual item as a string - item-2
                 */
                int nextId = id + 1;
                newItemString = JsonConvert.SerializeObject(GetItem(nextId));

                /*Create an item array*/
                arrayItem = Enumerable.Range(0, 3).Select(s => GetItem(nextId++)).ToArray();
                Task.WaitAll(new[]
                {
                    asyncCollector.AddAsync(GetItem(nextId++)),
                    asyncCollector.AddAsync(GetItem(nextId++)),
                    asyncCollector.AddAsync(GetItem(nextId++))
                });
            }


            [NoAutomaticTrigger]
            public static async Task Inputs(
                int id,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleItem, Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> itemOne,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameter2ValuesInArray, Connection = KustoConstants.DefaultConnectionStringName)] JArray itemTwo,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleItem, Connection = KustoConstants.DefaultConnectionStringName)] string itemThree,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithNoBoundParam, Connection = KustoConstants.DefaultConnectionStringName)] IAsyncEnumerable<Item> itemFour)
            {
                int itemId = id;
                // one item gets retrieved
                Assert.NotNull(itemOne);
                Assert.Single(itemOne);
                // There is only one item
                Assert.Equal(GetItem(itemId), itemOne.First());
                // There should be 2 items retrieved in this array. Not validating the contents though
                Assert.NotNull(itemTwo);
                Assert.Equal(2, itemTwo.Count);
                // The string retrieved for Item-3
                Assert.NotNull(itemThree);
                Assert.Equal(GetItem(id), JsonConvert.DeserializeObject<List<Item>>(itemThree).First());
                // Get all the values for 4
                Assert.NotNull(itemFour);
                await foreach (Item actualItem in itemFour.ConfigureAwait(false))
                {
                    // starting at 1 ensure we get all the items we need to get
                    Assert.NotNull(actualItem);
                    // All attributes based on ID should match
                    Assert.Equal(GetItem(actualItem.ID), actualItem);
                }
            }

            [NoAutomaticTrigger]
            public static void InputFail(
                int id,
#pragma warning disable IDE0060
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleItem, Connection = "KustoConnectionStringNoPermissions")] IEnumerable<Item> itemOne)
#pragma warning restore IDE0060
            {
                Assert.True(id > 0);
            }

            [NoAutomaticTrigger]
            public static void OutputFail(
            int id,
#pragma warning disable IDE0060
            [Kusto(Database: DatabaseName, TableName = TableName, Connection = "KustoConnectionStringNoPermissions")] IAsyncCollector<object> asyncCollector)
#pragma warning restore IDE0060
            {
                Assert.True(id > 0);
                // When we add an item it should fail with exception
                asyncCollector.AddAsync(GetItem(id));
            }


            [NoAutomaticTrigger]
            public static void InputMSI(
                int id,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterMSIItem, Connection = "KustoConnectionStringMSI", ManagedServiceIdentity = "system")] IEnumerable<Item> itemOne)
            {
                // one item gets retrieved
                Assert.NotNull(itemOne);
                Assert.Single(itemOne);
                // There is only one item
                Assert.Equal(GetItem(id + 999), itemOne.First());
            }

            [NoAutomaticTrigger]
            public static void OutputMSI(
                int id,
                [Kusto(Database: DatabaseName, TableName = TableName, Connection = "KustoConnectionStringMSI", ManagedServiceIdentity = "system")] out object newItem)
            {
                newItem = GetItem(id + 999);
            }

            private static Item GetItem(int id)
            {
                DateTime now = DateTime.UtcNow;
                return new Item
                {
                    ID = id,
                    Cost = id * 42.42,
                    Name = "Item-" + id,
                    // To be finite and check for precision
                    Timestamp = new DateTime(now.Year, now.Month, now.Day, Math.Min(id, 12), Math.Min(id, 59), Math.Min(id, 59), Math.Min(id, 999))
                };
            }
        }
    }
}
