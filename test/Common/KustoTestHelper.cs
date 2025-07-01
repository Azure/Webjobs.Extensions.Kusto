// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.



using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Kusto.Cloud.Platform.Data;
using Kusto.Data.Common;
using Kusto.Data.Data;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Newtonsoft.Json;


namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common
{
    internal sealed class KustoTestHelper
    {
        public const string DefaultTestConnectionString = "Data Source=https://kustofunctionscluster.eastus.dev.kusto.windows.net;Database=unittestdb;Fed=True;AppClientId=11111111-xxxx-xxxx-xxxx-111111111111;AppKey=appKey~appKey;Authority Id=1111111-1111-1111-1111-111111111111";
        public static KustoIngestContext CreateContext(IKustoIngestClient ingestClientService, string database = "unittest", string tableName = "items", string mappingRef = "", string dataFormat = "json")
        {
            var attribute = new KustoAttribute(database)
            {
                TableName = tableName,
                MappingRef = mappingRef,
                DataFormat = dataFormat
            };
            return new KustoIngestContext
            {
                IngestService = ingestClientService,
                ResolvedAttribute = attribute
            };
        }

        public static IDataReader MockResultDataReaderItems(string databaseName, string itemName, int counter)
        {
            var crp = new ClientRequestProperties();
            var kustoClientRequestDescriptor = new KustoClientRequestDescriptor("test-data-source", databaseName, "c-id" + counter);
            crp.SetParameter("name", itemName);
            DataSet testDataSet = PrepareDataSet(itemName, counter);
            DataTableReader testDataSetReader = testDataSet.CreateDataReader();
            var options = KustoDataReaderOptions.CreateFromClientRequestProperties(crp, kustoClientRequestDescriptor);
            return KustoJsonDataStream.CreateReaderWriterPairForTest(testDataSetReader, options);
        }
        private static DataSet PrepareDataSet(string itemName, int counter)
        {
            var set = new DataSet();
            for (int tableIndex = 0; tableIndex <= 2; tableIndex++)
            {
                DataTable table = ExtendedDataTable.Create<Row>("TableName_" + tableIndex);
                // Number of rows to generate
                var rows = new Row[2];
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = Row.CreateRandom(itemName, counter);
                    table.Rows.Add(rows[i].ToObjectArray());
                }
                set.Tables.Add(table);
            }
            set.AcceptChanges();
            return set;
        }

        public static List<Item> LoadItems(Stream stream)
        {
            var serializer = new JsonSerializer();
            var streamReader = new StreamReader(stream, new UTF8Encoding());
            var result = new List<Item>();
            using (var reader = new JsonTextReader(streamReader))
            {
                reader.CloseInput = false;
                reader.SupportMultipleContent = true;
                while (reader.Read())
                {
                    result.Add(serializer.Deserialize<Item>(reader));
                }
            }
            return result;
        }

        public static List<Item> LoadItems(string json)
        {
            var serializer = new JsonSerializer();
            var streamReader = new StringReader(json);
            var result = new List<Item>();
            using (var reader = new JsonTextReader(streamReader))
            {
                reader.CloseInput = false;
                reader.SupportMultipleContent = true;
                while (reader.Read())
                {
                    result.Add(serializer.Deserialize<Item>(reader));
                }
            }
            return result;
        }

        public static IConfiguration BuildConfiguration()
        {
            var values = new Dictionary<string, string>()
            {
                { KustoConstants.DefaultConnectionStringName, DefaultTestConnectionString },
                { "Attribute", "database=unittestdb;tableName=Items;Connection=KustoConnectionString" },
            };
            return TestHelpers.BuildConfiguration(values);
        }

        public static ExtensionConfigContext CreateExtensionConfigContext(INameResolver resolver)
        {
#pragma warning disable CS0618 // Cannot use var. IWebHookProvider is in Beta
            var mockWebHookProvider = new Mock<IWebHookProvider>();
            var mockExtensionRegistry = new Mock<IExtensionRegistry>();

            // TODO: ConverterManager needs to be fixed but this will work for now.
            IHost host = new HostBuilder()
                .ConfigureWebJobs()
                .Build();
            IConverterManager converterManager = host.Services.GetRequiredService<IConverterManager>();
            return new ExtensionConfigContext(BuildConfiguration(), resolver, converterManager, mockWebHookProvider.Object, mockExtensionRegistry.Object);
        }
    }
}