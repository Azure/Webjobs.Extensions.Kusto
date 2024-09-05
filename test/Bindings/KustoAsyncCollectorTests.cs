// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Utils;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests
{
    public class KustoAsyncCollectorTests
    {
        private readonly ILogger _logger = new LoggerFactory().CreateLogger<KustoAsyncCollectorTests>();

        [Fact]
        public async Task AddAsyncAccumulatesDataAsync()
        {
            // Set-up
            var mockIngestionClient = new Mock<IKustoIngestClient>(MockBehavior.Strict);
            KustoIngestContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context, this._logger);
            // when
            await collector.AddAsync(new Item { ID = 1, Name = "x" });
            // then - Verifies the ingestion service client is initialized and no calls are made to it yet
            mockIngestionClient.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task FlushAsyncIngestsDataAsync()
        {
            // Given
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
                Capture.In(actualIngestDataStreams),
                Capture.In(actualKustoIngestionProps),
                Capture.In(actualStreamSourceOptions))).ReturnsAsync(mockIngestionResult.Object);
            // When
            KustoIngestContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context, this._logger);
            IEnumerable<int> numberOfItems = Enumerable.Range(1, 5);
            var expectedItems = new List<Item>();
            var serializer = JsonSerializer.CreateDefault();
            // Add this test to make sure we do get Quoted strings
            var sb = new StringBuilder();
            using (var textWriter = new StringWriter(sb))
            {
                using var jsonWriter = new JsonTextWriter(textWriter) { QuoteName = true, Formatting = Formatting.Indented, CloseOutput = false };
                var taskResult = Task.WhenAll(numberOfItems.Select(i =>
                {
                    var item = new Item { ID = i, Name = "x-" + i };
                    expectedItems.Add(item);
                    if (i > 1)
                    {
                        textWriter.WriteLine(string.Empty);
                    }
                    serializer.Serialize(jsonWriter, item);
                    return collector.AddAsync(item);
                }));
            }
            await collector.FlushAsync();
            // Then
            // Validate the data
            var actualIngestDataStream = new StreamReader(actualIngestDataStreams.First(), new UTF8Encoding());
            string actualIngestDataText = actualIngestDataStream.ReadToEnd();
            List<Item> actualResultItems = KustoTestHelper.LoadItems(actualIngestDataText);
            Assert.True(expectedItems.SequenceEqual(actualResultItems));
            Assert.Equal(sb.ToString(), actualIngestDataText);
            // Validate ingestion properties used
            KustoIngestionProperties actualKustoIngestionProp = actualKustoIngestionProps.First();
            Assert.Equal("items", actualKustoIngestionProp.TableName);
            Assert.Equal("unittest", actualKustoIngestionProp.DatabaseName);
            Assert.Equal("multijson", actualKustoIngestionProp.Format.ToString());
            mockIngestionClient.VerifyAll();
        }
        [Fact]
        public async Task FlushAsyncIngestSingleRowAsync()
        {
            // Given
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
                Capture.In(actualIngestDataStreams),
                Capture.In(actualKustoIngestionProps),
                Capture.In(actualStreamSourceOptions))).ReturnsAsync(mockIngestionResult.Object);
            // When
            KustoIngestContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context, this._logger);
            var expectedItem = new Item { ID = 10, Name = "x-" + 10 };
            await collector.AddAsync(expectedItem);
            await collector.FlushAsync();
            // Then
            // Validate the data
            List<Item> actualItems = KustoTestHelper.LoadItems(actualIngestDataStreams.First());
            Assert.Single(actualItems);
            Assert.Equal(expectedItem, actualItems[0]);
            // Validate ingestion properties used
            KustoIngestionProperties actualKustoIngestionProp = actualKustoIngestionProps.First();
            Assert.Equal("items", actualKustoIngestionProp.TableName);
            Assert.Equal("unittest", actualKustoIngestionProp.DatabaseName);
            // Should be single JSON
            Assert.Equal("json", actualKustoIngestionProp.Format.ToString());
            mockIngestionClient.VerifyAll();
        }

    }
}