// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Kusto.Cloud.Platform.Utils;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Kusto;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests
{
    public class KustoAsyncCollectorTests
    {
        [Fact]
        public async Task AddAsyncAccumulatesDataAsync()
        {
            // Set-up
            var mockIngestionClient = new Mock<IKustoIngestClient>(MockBehavior.Strict);
            KustoContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context);
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
            KustoContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context);
            IEnumerable<int> numberOfItems = Enumerable.Range(1, 5);
            var expectedItems = new List<Item>();
            var taskResult = Task.WhenAll(numberOfItems.Select(i =>
            {
                var item = new Item { ID = i, Name = "x-" + i };
                expectedItems.Add(item);
                return collector.AddAsync(item);
            }));
            await collector.FlushAsync();
            // Then
            // Validate the data
            List<Item> actualResultItems = KustoTestHelper.LoadItems(actualIngestDataStreams.First());
            Assert.True(expectedItems.SequenceEqual(actualResultItems));
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
            KustoContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context);
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
