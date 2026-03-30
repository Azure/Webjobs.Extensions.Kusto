// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Kusto.Ingest;
using Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Common;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kusto.Tests.Bindings
{
    public class KustoIngestionServiceTests
    {
        private readonly ILogger _logger = new LoggerFactory().CreateLogger<KustoIngestionServiceTests>();

        /// <summary>
        /// When managed streaming ingestion falls back to queued (initial status = Queued),
        /// the collector should poll and report the final status (Succeeded).
        /// </summary>
        [Fact]
        public async Task ManagedIngestionPollsWhenFallingBackToQueued()
        {
            // Arrange
            var mockIngestionClient = new Mock<IKustoIngestClient>();
            var mockIngestionResult = new Mock<IKustoIngestionResult>();

            // First call returns Queued (fallback), second call returns Succeeded
            var queuedStatus = new IngestionStatus { Status = Status.Queued };
            var succeededStatus = new IngestionStatus { Status = Status.Succeeded };

            mockIngestionResult
                .SetupSequence(m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>()))
                .Returns(queuedStatus)
                .Returns(succeededStatus);

            mockIngestionClient
                .Setup(m => m.IngestFromStreamAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<KustoIngestionProperties>(),
                    It.IsAny<StreamSourceOptions>()))
                .ReturnsAsync(mockIngestionResult.Object);

            // Act — ingest through the collector which uses KustoManagedIngestionService
            KustoIngestContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context, this._logger);
            await collector.AddAsync(new Item { ID = 1, Name = "poll-test" });
            await collector.FlushAsync();

            // Assert — GetIngestionStatusBySourceId called at least twice (initial + poll)
            mockIngestionResult.Verify(
                m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>()),
                Times.AtLeast(2));
        }

        /// <summary>
        /// When managed streaming ingestion falls back to queued and the final polled status is Failed,
        /// the collector should throw a FunctionInvocationException.
        /// </summary>
        [Fact]
        public async Task ManagedIngestionThrowsOnPolledFailure()
        {
            // Arrange
            var mockIngestionClient = new Mock<IKustoIngestClient>();
            var mockIngestionResult = new Mock<IKustoIngestionResult>();

            var queuedStatus = new IngestionStatus { Status = Status.Queued };
            var failedStatus = new IngestionStatus { Status = Status.Failed };

            mockIngestionResult
                .SetupSequence(m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>()))
                .Returns(queuedStatus)
                .Returns(failedStatus);

            mockIngestionClient
                .Setup(m => m.IngestFromStreamAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<KustoIngestionProperties>(),
                    It.IsAny<StreamSourceOptions>()))
                .ReturnsAsync(mockIngestionResult.Object);

            // Act & Assert
            KustoIngestContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context, this._logger);
            await collector.AddAsync(new Item { ID = 2, Name = "fail-test" });

            FunctionInvocationException ex = await Assert.ThrowsAsync<FunctionInvocationException>(() => collector.FlushAsync());
            Assert.Contains("Ingestion status reported failure/partial success", ex.Message);
        }

        /// <summary>
        /// When managed ingestion returns Succeeded immediately (streaming worked),
        /// no polling should occur — GetIngestionStatusBySourceId called exactly once.
        /// </summary>
        [Fact]
        public async Task ManagedIngestionDoesNotPollOnImmediateSuccess()
        {
            // Arrange
            var mockIngestionClient = new Mock<IKustoIngestClient>();
            var mockIngestionResult = new Mock<IKustoIngestionResult>();

            var succeededStatus = new IngestionStatus { Status = Status.Succeeded };

            mockIngestionResult
                .Setup(m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>()))
                .Returns(succeededStatus);

            mockIngestionClient
                .Setup(m => m.IngestFromStreamAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<KustoIngestionProperties>(),
                    It.IsAny<StreamSourceOptions>()))
                .ReturnsAsync(mockIngestionResult.Object);

            // Act
            KustoIngestContext context = KustoTestHelper.CreateContext(mockIngestionClient.Object);
            var collector = new KustoAsyncCollector<Item>(context, this._logger);
            await collector.AddAsync(new Item { ID = 3, Name = "no-poll-test" });
            await collector.FlushAsync();

            // Assert — only the initial status check, no polling
            mockIngestionResult.Verify(
                m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>()),
                Times.Once);
        }

        /// <summary>
        /// Queued ingestion polls until a terminal state is reached.
        /// </summary>
        [Fact]
        public async Task QueuedIngestionPollsUntilSucceeded()
        {
            // Arrange
            var mockIngestionClient = new Mock<IKustoIngestClient>();
            var mockIngestionResult = new Mock<IKustoIngestionResult>();

            var pendingStatus = new IngestionStatus { Status = Status.Pending };
            var succeededStatus = new IngestionStatus { Status = Status.Succeeded };

            mockIngestionResult
                .SetupSequence(m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>()))
                .Returns(pendingStatus)
                .Returns(succeededStatus);

            mockIngestionClient
                .Setup(m => m.IngestFromStreamAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<KustoQueuedIngestionProperties>(),
                    It.IsAny<StreamSourceOptions>()))
                .ReturnsAsync(mockIngestionResult.Object);

            // Act — use queued ingestion type
            KustoIngestContext context = KustoTestHelper.CreateContext(
                mockIngestionClient.Object, ingestionType: "queued");
            var collector = new KustoAsyncCollector<Item>(context, this._logger);
            await collector.AddAsync(new Item { ID = 4, Name = "queued-test" });
            await collector.FlushAsync();

            // Assert — polled at least twice
            mockIngestionResult.Verify(
                m => m.GetIngestionStatusBySourceId(It.IsAny<Guid>()),
                Times.AtLeast(2));
        }
    }
}
