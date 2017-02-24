using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.Tests
{
    using Common;
    using States;

    [TestFixture]
    public class PipelineClientTests
    {
        protected Mock<IPipelineStorage> MockPipelineStorage;
        protected Mock<JobStorage> MockJobStorage;
        protected Mock<IBackgroundJobClient> MockJobClient;

        protected PipelineClient GetPipelineClient()
        {
            var pipelineClient = new PipelineClient(MockPipelineStorage.Object, MockJobClient.Object);
            return pipelineClient;
        }

        [SetUp]
        public void Setup()
        {
            MockPipelineStorage = new Mock<IPipelineStorage>();
            MockJobStorage = new Mock<JobStorage>();
            MockJobClient = new Mock<IBackgroundJobClient>();
        }

        [Test]
        public void Delete_JobContextIsNull_Throw()
        {
            // Arrange
            var pipelineClient = GetPipelineClient();
            var jobContext = new PipelineJobContext();

            // Act/Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => pipelineClient.DeleteAsync(jobContext));
        }

        [Test]
        public async Task Delete_CallHangfireClientDelete_ReturnJobContextWithNullHangfireId()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            jobContext.HangfireId = "hangfire-1";
            MockJobClient.Setup(client => client.ChangeState(jobContext.HangfireId,
                It.IsAny<DeletedState>(), It.IsAny<string>()))
                .Returns(true)
                .Verifiable();
            var pipelineClient = GetPipelineClient();

            // Act
            var res = await pipelineClient.DeleteAsync(jobContext);

            // Assert
            Assert.DoesNotThrow(() => MockJobClient.VerifyAll());
            Assert.IsNull(res.HangfireId);
        }

        [Test]
        public void Enqueue_JobContextIsNull_Throw()
        {
            // Arrange
            var pipelineClient = GetPipelineClient();
            var jobContext = new PipelineJobContext();

            // Act/Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => pipelineClient.EnqueueAsync(jobContext));
        }

        [Test]
        public async Task Enqueue_CallHangfireClientEnqueue_ReturnJobContextWithHangfireId()
        {
            var jobContext = new PipelineJobContext();
            jobContext.Id = "job-1";
            var expectedId = "hangfire-1";
            MockJobClient.Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()))
                .Returns(expectedId)
                .Verifiable();
            var pipelineClient = GetPipelineClient();

            // Act
            var res = await pipelineClient.EnqueueAsync(jobContext);

            // Assert
            Assert.DoesNotThrow(() => MockJobClient.VerifyAll());
            Assert.AreEqual(expectedId, res.HangfireId);
        }

        [Test]
        public void Requeue_ThrowIfHangfireIdIsNull()
        {
            // Arrange
            var pipelineClient = GetPipelineClient();
            var jobContext = new PipelineJobContext();

            // Act/Assert
            Assert.ThrowsAsync<ArgumentNullException>(() =>
                pipelineClient.RequeueAsync(jobContext));
        }

        [Test]
        public async Task Requeue_CallHangfireClientRequeue_ReturnJobContextWithHangfireId()
        {
            // Arrange
            var hangfireId = "hangfire-1";
            var jobContext = new PipelineJobContext();
            jobContext.HangfireId = hangfireId;
            MockJobClient.Setup(client => client.ChangeState(jobContext.HangfireId,
                It.IsAny<EnqueuedState>(), It.IsAny<string>()))
                    .Returns(true)
                    .Verifiable();
            var pipelineClient = GetPipelineClient();

            // Act
            var res = await pipelineClient.RequeueAsync(jobContext);

            // Assert
            Assert.DoesNotThrow(() => MockJobClient.VerifyAll());
            Assert.AreEqual(hangfireId, res.HangfireId);
        }

        [Test]
        public void ScheduleDateTimeOffset_ThrowIfJobContextIdIsNull()
        {
            // Arrange
            var pipelineClient = GetPipelineClient();
            var jobContext = new PipelineJobContext();
            var dateTimeOffset = DateTimeOffset.Now;

            // Act/Assert
            Assert.ThrowsAsync<ArgumentNullException>(() =>
                pipelineClient.ScheduleAsync(jobContext, dateTimeOffset));
        }

        [Test]
        public async Task ScheduleDateTimeOffset_CallHangfireClientSchedule_ReturnsJobContextWithHangfireJobId()
        {
            var jobContext = new PipelineJobContext();
            jobContext.Id = "job-1";
            var expectedId = "hangfire-1";
            MockJobClient.Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<ScheduledState>()))
                .Returns(expectedId)
                .Verifiable();
            var pipelineClient = GetPipelineClient();
            var dateTimeOffset = DateTimeOffset.Now;

            // Act
            var res = await pipelineClient.ScheduleAsync(jobContext, dateTimeOffset);

            // Assert
            Assert.DoesNotThrow(() => MockJobClient.VerifyAll());
            Assert.AreEqual(expectedId, res.HangfireId);
        }

        [Test]
        public void ScheduleTimeSpan_ThrowIfJobContextIdIsNull()
        {
            // Arrange
            var pipelineClient = GetPipelineClient();
            var jobContext = new PipelineJobContext();
            var timeSpan = TimeSpan.FromSeconds(1);

            // Act/Assert
            Assert.ThrowsAsync<ArgumentNullException>(() =>
                pipelineClient.ScheduleAsync(jobContext, timeSpan));
        }

        [Test]
        public async Task ScheduleTimeSpan_CallHangfireClientSchedule_ReturnsJobContextWithHangfireJobId()
        {
            var jobContext = new PipelineJobContext();
            jobContext.Id = "job-1";
            var expectedId = "hangfire-1";
            MockJobClient.Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<ScheduledState>()))
                .Returns(expectedId)
                .Verifiable();
            var pipelineClient = GetPipelineClient();
            var timeSpan = TimeSpan.FromSeconds(1);

            // Act
            var res = await pipelineClient.ScheduleAsync(jobContext, timeSpan);

            // Assert
            Assert.DoesNotThrow(() => MockJobClient.VerifyAll());
            Assert.AreEqual(expectedId, res.HangfireId);
        }
    }
}
