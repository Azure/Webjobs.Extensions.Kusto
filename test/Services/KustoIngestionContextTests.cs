// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Kusto;
using Moq;
using Xunit;


namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Services
{
    public class KustoIngestContextTests
    {

        private readonly string CustomIngestionProperties = "@flushImmediately=true,@pollTimeoutMinutes=2,@pollIntervalSeconds=30";

        [Fact]
        public async Task PollIngestionStatusShouldReturnCompletedStatus()
        {
            // Arrange
            var sourceId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();
            var expectedStatus = new IngestionStatus { Status = Status.Succeeded };
            var expectedProperties = new KustoIngestionProperties
            {
                DatabaseName = "TestDatabase",
                TableName = "TestTable",
            };
            var streamSourceOptions = new StreamSourceOptions
            {
                SourceId = sourceId,
            };
            var mockIngestService = new Mock<IKustoIngestClient>();
            var kustoIngestContext = new KustoIngestContext
            {
                IngestService = mockIngestService.Object,
                ResolvedAttribute = new KustoAttribute("TestDatabase")
                {
                    TableName = "TestTable",
                    IngestionType = "queued",
                    IngestionProperties = CustomIngestionProperties,
                }
            };
            var mockIngestionResult = new Mock<IKustoIngestionResult>();
            var succeededStatus = new IngestionStatus { Status = Status.Succeeded };
            var queuedStatus = new IngestionStatus { Status = Status.Queued };
            var pendingStatus = new IngestionStatus { Status = Status.Pending };

            mockIngestionResult.SetupSequence(service => service.GetIngestionStatusBySourceId(It.IsAny<Guid>()))
                .Returns(pendingStatus)
                .Returns(queuedStatus)
                .Returns(succeededStatus);

            Guid actualId = Guid.Empty;
            KustoIngestionProperties actualKip = null;
            mockIngestService
                .Setup(service => service.IngestFromStreamAsync(It.IsAny<Stream>(), It.IsAny<KustoIngestionProperties>(), It.IsAny<StreamSourceOptions>()))
                .ReturnsAsync(mockIngestionResult.Object)
                .Callback<Stream, KustoIngestionProperties, StreamSourceOptions>((stream, properties, options) =>
                {
                    actualId = options.SourceId;
                    actualKip = properties;
                });

            // Act
            IngestionStatus result = await kustoIngestContext.IngestData(DataSourceFormat.json, new MemoryStream(), streamSourceOptions, cancellationToken);

            // Assert
            mockIngestionResult.Verify(mock => mock.GetIngestionStatusBySourceId(It.IsAny<Guid>()), Times.AtLeast(3));
            Assert.Equal(actualKip.DatabaseName, expectedProperties.DatabaseName);
            Assert.Equal(actualKip.TableName, expectedProperties.TableName);
            Assert.Equal(actualId, sourceId);
            Assert.True(((KustoQueuedIngestionProperties)actualKip).FlushImmediately);
            Assert.Equal(Status.Succeeded, result.Status);
        }
    }
}