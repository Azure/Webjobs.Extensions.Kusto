// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Globalization;
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
        private const string MappingName = "product_to_item_json_mapping";
        private const string NonExistingMappingName = "a_mapping_that_does_not_exist";
        // Create the table
        private readonly string CreateItemTable = $".create-merge table {TableName}(ID:int,Name:string, Cost:double,Timestamp:datetime)";
        private readonly string CreateTableMappings = $".create-or-alter table {TableName} ingestion json mapping \"{MappingName}\" '[{{\"column\":\"ID\",\"path\":\"$.ProductID\",\"datatype\":\"\",\"transform\":null}},{{\"column\":\"Name\",\"path\":\"$.ProductName\",\"datatype\":\"\",\"transform\":null}},{{\"column\":\"Cost\",\"path\":\"$.UnitCost\",\"datatype\":\"\",\"transform\":null}},{{\"column\":\"Timestamp\",\"path\":\"$.Timestamp\",\"datatype\":\"\",\"transform\":null}}]'";
        private readonly string DropTableMappings = $".drop table {TableName} ingestion json mapping \"{MappingName}\"";
        private readonly string ClearItemTable = $".clear table {TableName} data";
        private readonly string DropTable = $".drop table {TableName}";
        // Queries for input binding with parameters
        private const string QueryWithBoundParam = "declare query_parameters(startId:int,endId:int);kusto_functions_e2e_tests | where ID >= startId and ID <= endId and ingestion_time()>ago(10s)";
        // Queries for input binding without parameters
        private const string QueryWithNoBoundParam = "kusto_functions_e2e_tests| where ingestion_time() > ago(10s) | order by ID asc";
        // Make sure that the InitialCatalog parameter in the tests has the same value as the Database name
        private const string DatabaseName = "e2e";
        private const int startId = 1;
        // Query parameter to get a single row where start and end are the same
        private const string KqlParameterSingleItem = "@startId=1,@endId=1";
        private const string KqlParameterSingleItemCsv = "@startId=8,@endId=8";
        private const string KqlParameter2ValuesInArray = "@startId=6,@endId=7";
        private const string KqlParameterMSIItem = "@startId=1000,@endId=1000";
        private const string KqlParameterSingleCsv = "@startId=2000,@endId=2000";
        private const string KqlParameterCSVItems = "@startId=2000,@endId=2010";
        private const string KqlParameterMappedProducts = "@startId=3000,@endId=3000";
        // A client to perform all the assertions
        protected ICslQueryProvider KustoQueryClient { get; private set; }
        protected ICslAdminProvider KustoAdminClient { get; private set; }
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
        private readonly TestLoggerProvider _loggerProvider = new();

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
            // Create mappings
            this.KustoAdminClient.ExecuteControlCommand(this.CreateTableMappings);
            Assert.NotNull(tableCreationResult);
            var parameter = new Dictionary<string, object>
            {
                ["id"] = startId
            };
            // Output binding tests
            await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.Outputs), parameter);
            // Validate all rows written in output bindings can be queries
            await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.Inputs), parameter);
            // Fail scenario for no read privileges
            Exception readPrivilegeException = await Record.ExceptionAsync(() => jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.InputFailForUserWithNoIngestPrivileges), parameter));
            Assert.IsType<FunctionInvocationException>(readPrivilegeException);
            Assert.Contains("Forbidden (403-Forbidden)", readPrivilegeException.GetBaseException().Message);

            // Fail scenario for no ingest privileges
            Exception ingestPrivilegeException = await Record.ExceptionAsync(() => jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.OutputFailForUserWithNoReadPrivileges), parameter));
            Assert.IsType<FunctionInvocationException>(ingestPrivilegeException);
            Assert.NotEmpty(ingestPrivilegeException.GetBaseException().Message);
            string actualExceptionCause = ingestPrivilegeException.GetBaseException().Message;
            Assert.Contains("Forbidden (403-Forbidden)", actualExceptionCause);

            // Tests for managed service identity disabled for local runs
            /*
            string tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            string appId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            string appSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
            {
                logger.LogWarning("Environment variables AZURE_TENANT_ID/AZURE_CLIENT_ID/AZURE_CLIENT_SECRET are not set. MSI tests will not be run");
            }
            else
            {
                // Tests for managed service identity
                await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.OutputMSI), parameter);
                await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.InputMSI), parameter);
            }
            */
            // Tests where the exceptions are caused due to invalid strings
            string[] testsToExecute = { nameof(KustoEndToEndTestClass.InputFailInvalidConnectionString), nameof(KustoEndToEndTestClass.OutputFailInvalidConnectionString) };
            foreach (string test in testsToExecute)
            {
                Exception invalidConnectionStringException = await Record.ExceptionAsync(() => jobHost.GetJobHost().CallAsync(test, parameter));
                Assert.IsType<FunctionInvocationException>(invalidConnectionStringException);
                Assert.Equal("Kusto Connection String Builder has some invalid or conflicting properties: Specified 'AAD application key' authentication method has some incorrect properties. Missing: [Application Key,Authority Id].. ',\r\nPlease consult Kusto Connection String documentation at https://docs.microsoft.com/en-us/azure/kusto/api/connection-strings/kusto", invalidConnectionStringException.GetBaseException().Message);
            }
            // Tests for managed CSV
            await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.OutputsCSV), parameter);
            await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.InputsCSV), parameter);

            // Tests for records with mapping
            await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.OutputsWithMapping), parameter);
            await jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.InputWithMapping), parameter);

            // Tests for the case where this is a bad JSON. This will cause an ingest failure
            string[] invalidJsonTests = { nameof(KustoEndToEndTestClass.OutputsWithInvalidJson), nameof(KustoEndToEndTestClass.OutputMixedJsonFailure) };
            foreach (string test in invalidJsonTests)
            {
                Exception invalidOutputsException = await Record.ExceptionAsync(() => jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.OutputsWithInvalidJson), parameter));
                Assert.IsType<FunctionInvocationException>(invalidOutputsException);
                var actualExceptionMessageJson = JObject.Parse(invalidOutputsException.GetBaseException().Message);
                string actualMessage = (string)actualExceptionMessageJson["error"]["message"];
                string actualMessageValue = (string)actualExceptionMessageJson["error"]["@message"];
                string actualType = (string)actualExceptionMessageJson["error"]["@type"];
                bool isPermanent = (bool)actualExceptionMessageJson["error"]["@permanent"];
                Assert.Equal("Request is invalid and cannot be executed.", actualMessage);
                Assert.Equal("Kusto.DataNode.Exceptions.StreamingIngestionRequestException", actualType);
                Assert.Equal($"Bad streaming ingestion request to {DatabaseName}.{TableName} : The input stream is empty after processing, tip:check stream validity", actualMessageValue);
                Assert.True(isPermanent);
            }
            // A case where ingestion is done , but there exists no such mapping causing ingestion failure
            Exception noSuchMappingException = await Record.ExceptionAsync(() => jobHost.GetJobHost().CallAsync(nameof(KustoEndToEndTestClass.OutputsWithMappingFailIngestion), parameter));
            Assert.IsType<FunctionInvocationException>(noSuchMappingException);
            string baseMessage = noSuchMappingException.GetBaseException().Message;
            Assert.Equal($"Entity ID '{NonExistingMappingName}' of kind 'MappingPersistent' was not found.", baseMessage);
            /*
            // To debug further, uncomment the following lines. The logs would be available in test\bin\Debug\netcoreapp3.1
            IEnumerable<LogMessage> allLoggedMessages = this._loggerProvider.GetAllLogMessages();
            foreach (LogMessage logMessage in allLoggedMessages)
            {
                System.IO.File.AppendAllText("logs-created.txt", logMessage.FormattedMessage + Environment.NewLine);
            }
            */
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
                    logging.SetMinimumLevel(LogLevel.Trace);
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
            _ = this.KustoAdminClient.ExecuteControlCommandAsync(DatabaseName, this.DropTableMappings);
            _ = this.KustoAdminClient.ExecuteControlCommandAsync(DatabaseName, this.DropTable);
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
                [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName, DataFormat = "csv")] out object newItemCsv,
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

                /*
                    Csv test for the data
                */
                Item csvItem = GetItem(nextId++);
                newItemCsv = $"{csvItem.ID},{csvItem.Name},{csvItem.Cost},{csvItem.Timestamp.ToUtcString(CultureInfo.InvariantCulture)}";
            }


            [NoAutomaticTrigger]
            public static async Task Inputs(
                int id,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleItem, Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> itemOne,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameter2ValuesInArray, Connection = KustoConstants.DefaultConnectionStringName)] JArray itemTwo,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleItem, Connection = KustoConstants.DefaultConnectionStringName)] string itemThree,
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleItemCsv, Connection = KustoConstants.DefaultConnectionStringName)] string itemEight,
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
                // Special CSV - Item number 8
                Assert.NotNull(itemEight);
                Assert.Equal(GetItem(8), JsonConvert.DeserializeObject<List<Item>>(itemEight).First());

            }

            [NoAutomaticTrigger]
            public static void InputFailForUserWithNoIngestPrivileges(
                int id,
#pragma warning disable IDE0060
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleItem, Connection = "KustoConnectionStringNoPermissions")] IEnumerable<Item> itemOne)
#pragma warning restore IDE0060
            {
                Assert.True(id > 0);
            }

            [NoAutomaticTrigger]
            public static void InputFailInvalidConnectionString(
                int id,
#pragma warning disable IDE0060
                [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleItem, Connection = "KustoConnectionStringInvalidAttributes")] IEnumerable<Item> itemOne)
#pragma warning restore IDE0060
            {
                Assert.True(id > 0);
            }

            [NoAutomaticTrigger]
            public static void OutputFailInvalidConnectionString(
            int id,
#pragma warning disable IDE0060
            [Kusto(Database: DatabaseName, TableName = TableName, Connection = "KustoConnectionStringInvalidAttributes")] out object itemOne)
#pragma warning restore IDE0060
            {
                Assert.True(id > 0);
                itemOne = GetItem(id + 999);
                // one item gets retrieved
                Assert.Null(itemOne);
            }


            [NoAutomaticTrigger]
            public static void OutputFailForUserWithNoReadPrivileges(
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


            [NoAutomaticTrigger]
            public static void OutputsCSV(
            int id,
            [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName, DataFormat = "csv")] out object csvItem,
            [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName, DataFormat = "csv")] IAsyncCollector<object> csvItemsCollector)
            {
                /*
                 Add an individual item-1 csv
                 */
                int csvNextId = id + 1999;
                csvItem = GetItemCsv(csvNextId);
                csvNextId++;
                Task.WaitAll(new[]
                {
                    csvItemsCollector.AddAsync(GetItemCsv(csvNextId)),
                    csvItemsCollector.AddAsync(GetItemCsv(csvNextId++)),
                    csvItemsCollector.AddAsync(GetItemCsv(csvNextId++))
                });
            }

            [NoAutomaticTrigger]
            public static async Task InputsCSV(
            int id,
            [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterSingleCsv, Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> csvItemOne,
            [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterCSVItems, Connection = KustoConstants.DefaultConnectionStringName)] IAsyncEnumerable<Item> csvItemCollection)
            {
                // Validate all the CSV records
                int csvNextId = id + 1999;
                // one item gets retrieved
                Assert.NotNull(csvItemOne);
                Assert.Single(csvItemOne);
                // There is only one item
                Assert.Equal(GetItem(csvNextId), csvItemOne.First());
                // Get all the values for 4
                Assert.NotNull(csvItemCollection);
                await foreach (Item actualItem in csvItemCollection.ConfigureAwait(false))
                {
                    // starting at 1 ensure we get all the items we need to get
                    Assert.NotNull(actualItem);
                    // All attributes based on ID should match
                    Assert.Equal(GetItem(actualItem.ID), actualItem);
                }
            }

            [NoAutomaticTrigger]
            public static void OutputsWithMapping(
            int id,
            [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName, MappingRef = MappingName)] out string product)
            {
                int productId = id + 2999;
                product = GetProductJson(productId);
            }

            [NoAutomaticTrigger]
            public static void InputWithMapping(
            int id,
            [Kusto(Database: DatabaseName, KqlCommand = QueryWithBoundParam, KqlParameters = KqlParameterMappedProducts, Connection = KustoConstants.DefaultConnectionStringName)] IEnumerable<Item> productOne)
            {
                int productId = id + 2999;
                // Validate all the retrieved records
                // one item gets retrieved
                Assert.NotNull(productOne);
                Assert.Single(productOne);
                // There is only one item
                Assert.Equal(GetItem(productId), productOne.First());
            }


            [NoAutomaticTrigger]
            public static void OutputsWithMappingFailIngestion(
            int id,
            [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName, MappingRef = NonExistingMappingName)] out string product)
            {
                int productId = id + 2999;
                product = GetProductJson(productId);
            }

            [NoAutomaticTrigger]
            public static void OutputMixedJsonFailure(
            int id,
            [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName)] out object mixedJson)
            {
                /*
                 Add a mixed case where we have a product and Item
                 */
                int nextItemId = id + 3999;
                string product = GetProductJson(nextItemId);
                nextItemId++;
                string item = JsonConvert.SerializeObject(GetItem(nextItemId));
                mixedJson = $"[{product},{item}]";
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

            [NoAutomaticTrigger]
            public static void OutputsWithInvalidJson(
            int id,
            [Kusto(Database: DatabaseName, TableName = TableName, Connection = KustoConstants.DefaultConnectionStringName, MappingRef = MappingName)] out string product)
            {
                int productId = id + 2999;
                string productJson = GetProductJson(productId);
                // Create a JSON that is invalid! This should throw a payload exception
                product = productJson[1..];
            }

            private static string GetProductJson(int id)
            {
                DateTime now = DateTime.UtcNow;
                dynamic product = new JObject();
                product.ProductID = id;
                product.ProductName = "Item-" + id;
                product.UnitCost = id * 42.42;
                product.Timestamp = new DateTime(now.Year, now.Month, now.Day, Math.Min(id, 12), Math.Min(id, 59), Math.Min(id, 59), Math.Min(id, 999));
                string result = product.ToString(Formatting.None);
                return result;
            }

            private static string GetItemCsv(int id)
            {
                DateTime now = DateTime.UtcNow;
                string timestamp = new DateTime(now.Year, now.Month, now.Day, Math.Min(id, 12), Math.Min(id, 59), Math.Min(id, 59), Math.Min(id, 999)).ToUtcString(CultureInfo.InvariantCulture);
                // is ordinal based in the table with this order
                return $"{id},Item-{id},{id * 42.42},{timestamp}";
            }
        }
    }
}