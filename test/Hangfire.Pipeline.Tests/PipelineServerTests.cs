using Moq;
using Moq.Language.Flow;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.Tests
{
    [TestFixture]
    public class PipelineServerTests
    {
        private const string JobId = "job-1";
        private Mock<IPipelineStorage> MockStorage;
        private Mock<IPipelineTaskFactory> MockTaskFactory;
        private IPipelineJobContext JobContext;
        private IJobCancellationToken Jct;

        [SetUp]
        public void Setup()
        {
            MockStorage = new Mock<IPipelineStorage>();
            MockTaskFactory = new Mock<IPipelineTaskFactory>();
            JobContext = new PipelineJobContext();
            Jct = new JobCancellationToken(false);
        }

        protected PipelineServer GetPipelineServer()
        {
            MockStorage.Setup(storage => storage.GetJobContextAsync(JobId,
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(JobContext)
                    .Verifiable();
            var pipelineServer = new PipelineServer(MockStorage.Object, MockTaskFactory.Object);
            return pipelineServer;
        }

        protected ISetup<IPipelineTask, Task<IPipelineTaskContext>> GetMockTaskSetup(
            Mock<IPipelineTask> mockTask)
        {
            var setup = mockTask.Setup(task => task.ExecuteTaskAsync(
                It.IsAny<IPipelineTaskContext>(), It.IsAny<IPipelineJobContext>(),
                It.IsAny<IPipelineStorage>(), It.IsAny<CancellationToken>()));
            return setup;
        }

        [Test]
        public void ExecuteJob_GetJobContextFromStorage()
        {
            // Arrange
            MockStorage.Setup(storage => storage.UpdateJobContextAsync(JobContext,
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();
            var task = "task1";
            var taskContext = new PipelineTaskContext { Task = task, Id = task };
            var mockTask = new Mock<IPipelineTask>();
            var mockTaskSetup = GetMockTaskSetup(mockTask);
            mockTaskSetup.ReturnsAsync(taskContext);
            MockTaskFactory.Setup(factory => factory.Create(task))
                .Returns(mockTask.Object);
            var pipelineServer = GetPipelineServer();
            JobContext.QueueTask(taskContext);

            // Act
            pipelineServer.ExecuteJob(JobId, Jct);

            // Assert
            Assert.DoesNotThrow(() => MockStorage.VerifyAll());
        }

        [Test]
        public void ExecuteJob_JobContextFromStorageIsNull_Throw()
        {
            // Arrange
            var pipelineServer = new PipelineServer(MockStorage.Object, MockTaskFactory.Object);

            // Act/Assert
            Assert.Throws<NullReferenceException>(() =>
                pipelineServer.ExecuteJob(JobId, Jct));
        }

        [Test]
        public void ExecuteJob_BeforeExecutingAnyTasks_JobContextStartTimeIsRecordedAndUpdated()
        {
            // Arrange
            MockStorage.Setup(storage => storage.UpdateJobContextAsync(JobContext,
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Verifiable();
            var task = "task1";
            var taskContext = new PipelineTaskContext { Task = task, Id = task };
            var mockTask = new Mock<IPipelineTask>();
            var mockTaskSetup = GetMockTaskSetup(mockTask);
            mockTaskSetup.ReturnsAsync(taskContext);
            MockTaskFactory.Setup(factory => factory.Create(task))
                .Returns(mockTask.Object);
            var pipelineServer = GetPipelineServer();
            JobContext.QueueTask(taskContext);

            // Act
            pipelineServer.ExecuteJob(JobId, Jct);

            // Assert
            Assert.DoesNotThrow(() => MockStorage.VerifyAll());
        }

        [Test]
        public void ExecuteJob_IfCancellationTokenIsCanceled_ThrowBeforeExecutingTasks()
        {
            // Arrange
            var pipelineServer = GetPipelineServer();
            var cancelledJct = new JobCancellationToken(true);
            JobContext.QueueTask(new PipelineTaskContext { Task = "task1", Id = "task1" });

            // Act/Assert
            Assert.Throws<OperationCanceledException>(() =>
                pipelineServer.ExecuteJob(JobId, cancelledJct));
        }

        [Test]
        public void ExecuteJob_BeforeCreatingTaskInstance_GetDependencyScope()
        {
            // Arrange
            var taskName = "task1";
            var hasDependencyScope = false;
            var hasCreatedTaskInstance = false;
            var taskContext = new PipelineTaskContext { Task = taskName, Id = taskName };
            MockTaskFactory.Setup(factory => factory.GetScope()).Callback(() =>
            {
                if (hasCreatedTaskInstance)
                    Assert.Fail("Task instance created outside of dependency scope");
                hasDependencyScope = true;
            });
            var mockTask = new Mock<IPipelineTask>();
            var mockTaskSetup = GetMockTaskSetup(mockTask);
            mockTaskSetup.ReturnsAsync(taskContext);
            MockTaskFactory.Setup(factory => factory.Create(taskName))
                .Returns(mockTask.Object)
                .Callback(() => hasCreatedTaskInstance = true);
            var pipelineServer = GetPipelineServer();
            JobContext.QueueTask(taskContext);

            // Act
            pipelineServer.ExecuteJob(JobId, Jct);

            // Assert
            Assert.IsTrue(hasDependencyScope);
            Assert.IsTrue(hasCreatedTaskInstance);
        }

        [Test]
        public void ExecuteJob_TaskContextDoesNotHaveTaskName_Throw()
        {
            // Arrange
            var pipelineServer = GetPipelineServer();
            JobContext.QueueTask(new PipelineTaskContext());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                pipelineServer.ExecuteJob(JobId, Jct));
        }

        [Test]
        public void ExecuteJob_TaskContextDoesNotHaveTaskId_Throw()
        {
            // Arrange
            var pipelineServer = GetPipelineServer();
            JobContext.QueueTask(new PipelineTaskContext { Task = "task" });

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
                pipelineServer.ExecuteJob(JobId, Jct));
        }

        [Test]
        public void ExecuteJob_DoNotRunTaskThatHasAlreadyBeenCompleted()
        {
            // Arrange
            var task = "task1";
            JobContext.QueueTask(new PipelineTaskContext
            {
                Task = task,
                Id = task,
                End = DateTime.UtcNow
            });
            MockTaskFactory.Setup(factory => factory.Create(task)).Verifiable();
            var pipelineServer = GetPipelineServer();

            // Act
            pipelineServer.ExecuteJob(JobId, Jct);

            // Assert
            Assert.That(() => MockTaskFactory.VerifyAll(), Throws.Exception);
        }

        [Test]
        public void ExecuteJob_NonParallelTasksWaitForPreviousTasksToCompleteAndBlockSubsequentTasks()
        {
            // Arrange

            // Task 1 (parallel)
            DateTime task1Start = DateTime.MinValue;
            DateTime task1End = DateTime.MinValue;
            var task1 = new PipelineTaskContext
            {
                Id = "task1",
                Task = "task1",
                RunParallel = true
            };
            var mockTask1 = new Mock<IPipelineTask>();
            var mockTask1Setup = GetMockTaskSetup(mockTask1);
            mockTask1Setup.ReturnsAsync(task1);
            MockTaskFactory.Setup(factory => factory.Create(task1.Task)).Returns(mockTask1.Object);

            // Task 2 (parallel)
            DateTime task2Start = DateTime.MinValue;
            DateTime task2End = DateTime.MinValue;
            var task2 = new PipelineTaskContext
            {
                Id = "task2",
                Task = "task2",
                RunParallel = true
            };
            var mockTask2 = new Mock<IPipelineTask>();
            var mockTask2Setup = GetMockTaskSetup(mockTask2);
            mockTask2Setup.ReturnsAsync(task2);
            MockTaskFactory.Setup(factory => factory.Create(task2.Task))
                .Returns(mockTask2.Object);

            // Task 3 (blocking)
            DateTime task3Start = DateTime.MinValue;
            DateTime task3End = DateTime.MinValue;
            var task3 = new PipelineTaskContext
            {
                Id = "task3",
                Task = "task3",
                RunParallel = false
            };
            var mockTask3 = new Mock<IPipelineTask>();
            var mockTask3Setup = GetMockTaskSetup(mockTask3);
            mockTask3Setup.ReturnsAsync(task3);
            MockTaskFactory.Setup(factory => factory.Create(task3.Task))
                .Returns(mockTask3.Object);

            // Task 4 (parallel)
            DateTime task4Start = DateTime.MinValue;
            DateTime task4End = DateTime.MinValue;
            var task4 = new PipelineTaskContext
            {
                Id = "task4",
                Task = "task4",
                RunParallel = true
            };
            var mockTask4 = new Mock<IPipelineTask>();
            var mockTask4Setup = GetMockTaskSetup(mockTask4);
            mockTask4Setup.ReturnsAsync(task4);
            MockTaskFactory.Setup(factory => factory.Create(task4.Task))
                .Returns(mockTask4.Object);

            // Intecept job context updates to get task start and end times
            MockStorage.Setup(storage => storage.UpdateJobContextAsync(JobContext,
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Callback<IPipelineJobContext, CancellationToken>((jobContext, ct) =>
                    {
                        if (jobContext.Completed == null)
                            return;
                        foreach (var completed in jobContext.Completed)
                        {
                            if (completed.Id == task1.Id)
                            {
                                task1Start = task1.Start;
                                task1End = task1.End;
                            }
                            if (completed.Id == task2.Id)
                            {
                                task2Start = task2.Start;
                                task2End = task2.End;
                            }
                            if (completed.Id == task3.Id)
                            {
                                task3Start = task3.Start;
                                task3End = task3.End;
                            }
                            if (completed.Id == task4.Id)
                            {
                                task4Start = task4.Start;
                                task4End = task4.End;
                            }
                        }
                    });

            // Queue tasks in job context
            JobContext.QueueTask(task1);
            JobContext.QueueTask(task2);
            JobContext.QueueTask(task3);
            JobContext.QueueTask(task4);
            var pipelineServer = GetPipelineServer();

            // Act
            pipelineServer.ExecuteJob(JobId, Jct);

            // Assert
            Assert.IsTrue(task1Start <= task3Start);
            Assert.IsTrue(task2Start <= task3Start);
            Assert.IsTrue(task1End <= task3Start);
            Assert.IsTrue(task2End <= task3Start);
            Assert.IsTrue(task3Start <= task4Start);
            Assert.IsTrue(task3End <= task4Start);
        }

        [Test]
        public void ExecuteJob_TaskInstanceIsReleasedByTaskFactory()
        {
            // Arrange
            var task = "task1";
            var taskContext = new PipelineTaskContext
            {
                Task = task,
                Id = task
            };
            var mockTask = new Mock<IPipelineTask>();
            var mockTaskSetup = GetMockTaskSetup(mockTask);
            mockTaskSetup.ReturnsAsync(taskContext);
            JobContext.QueueTask(taskContext);
            MockTaskFactory.Setup(factory => factory.Create(task)).Returns(mockTask.Object)
                .Verifiable();
            MockTaskFactory.Setup(factory => factory.Release(mockTask.Object))
                .Verifiable();
            var pipelineServer = GetPipelineServer();

            // Act
            pipelineServer.ExecuteJob(JobId, Jct);

            // Assert
            Assert.DoesNotThrow(() => MockTaskFactory.VerifyAll());
        }

        [Test]
        public void ExecuteJob_AfterTaskIsExecuted_TaskExecutionEndTimeIsRecordedAndJobContextIsUpdated()
        {
            // Arrange
            DateTime taskEnd = DateTime.MinValue;
            var task = "task1";
            var taskContext = new PipelineTaskContext
            {
                Task = task,
                Id = task
            };
            var mockTask = new Mock<IPipelineTask>();
            var mockTaskSetup = GetMockTaskSetup(mockTask);
            mockTaskSetup.ReturnsAsync(taskContext);
            JobContext.QueueTask(taskContext);
            MockTaskFactory.Setup(factory => factory.Create(task)).Returns(mockTask.Object);
            MockStorage.Setup(storage => storage.UpdateJobContextAsync(JobContext,
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Callback<IPipelineJobContext, CancellationToken>((jobContext, ct) =>
                    {
                        if (jobContext.Completed == null)
                            return;
                        var innerTask = jobContext.Completed.First(completed =>
                            completed.Id == task);
                        taskEnd = innerTask.End;
                    }).Verifiable();
            var pipelineServer = GetPipelineServer();

            // Act
            pipelineServer.ExecuteJob(JobId, Jct);

            // Assert
            Assert.AreNotEqual(DateTime.MinValue, taskEnd);
            Assert.DoesNotThrow(() => MockStorage.VerifyAll());
        }

        [Test]
        public void ExecuteJob_IfExceptionIsThrownDuringTaskExecution_JobBreaks()
        {
            // Arrange
            DateTime jobEnd = DateTime.MinValue;
            var task1 = new PipelineTaskContext
            {
                Id = "task1",
                Task = "task1",
                RunParallel = false
            };
            var mockTask1 = new Mock<IPipelineTask>();
            var mockTask1Setup = GetMockTaskSetup(mockTask1);
            mockTask1Setup.ReturnsAsync(task1);
            mockTask1Setup.Throws(new Exception());
            MockTaskFactory.Setup(factory => factory.Create(task1.Task))
                .Returns(mockTask1.Object);

            // Task 2 (parallel)
            var task2 = new PipelineTaskContext
            {
                Id = "task2",
                Task = "task2",
                RunParallel = false
            };
            var mockTask2 = new Mock<IPipelineTask>();
            var mockTask2Setup = GetMockTaskSetup(mockTask2);
            mockTask2Setup.ReturnsAsync(task2);
            mockTask2Setup.Verifiable();
            MockTaskFactory.Setup(factory => factory.Create(task2.Task))
                .Returns(mockTask2.Object);

            MockStorage.Setup(storage => storage.UpdateJobContextAsync(JobContext,
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Callback<IPipelineJobContext, CancellationToken>((jobContext, ct) =>
                    {
                        jobEnd = jobContext.End;
                    }).Verifiable();

            // Queue tasks
            JobContext.QueueTask(task1);
            JobContext.QueueTask(task2);
            var pipelineServer = GetPipelineServer();

            // Act/Assert
            Assert.Throws<Exception>(() =>
                pipelineServer.ExecuteJob(JobId, Jct));
            Assert.That(() => mockTask2.VerifyAll(), Throws.Exception);
            Assert.AreEqual(DateTime.MinValue, jobEnd);
        }

        [Test]
        public void ExecuteJob_AfterAllTasksAreComplete_JobContextEndTimeIsRecordedAndUpdated()
        {
            // Arrange
            DateTime jobEnd = DateTime.MinValue;
            var task = "task1";
            var taskContext = new PipelineTaskContext
            {
                Task = task,
                Id = task
            };
            var mockTask = new Mock<IPipelineTask>();
            var mockTaskSetup = GetMockTaskSetup(mockTask);
            mockTaskSetup.ReturnsAsync(taskContext);
            JobContext.QueueTask(taskContext);
            MockTaskFactory.Setup(factory => factory.Create(task)).Returns(mockTask.Object);
            MockStorage.Setup(storage => storage.UpdateJobContextAsync(JobContext,
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Callback<IPipelineJobContext, CancellationToken>((jobContext, ct) =>
                    {
                        jobEnd = jobContext.End;
                    }).Verifiable();
            var pipelineServer = GetPipelineServer();

            // Act
            pipelineServer.ExecuteJob(JobId, Jct);

            // Assert
            Assert.AreNotEqual(DateTime.MinValue, jobEnd);
            Assert.DoesNotThrow(() => MockStorage.VerifyAll());
        }

        [Test]
        public void ExecuteJob_CancelledJobAlsoCancelsInternalCancellationToken()
        {
            // Arrange
            var task = "task1";
            var taskContext = new PipelineTaskContext
            {
                Task = task,
                Id = task
            };
            var mockTask = new Mock<IPipelineTask>();
            var mockTaskSetup = GetMockTaskSetup(mockTask);
            var mockJct = new Mock<IJobCancellationToken>();
            mockTaskSetup.ReturnsAsync(taskContext);
            JobContext.QueueTask(taskContext);
            MockTaskFactory.Setup(factory => factory.Create(task)).Returns(mockTask.Object);
            MockStorage.Setup(storage => storage.UpdateJobContextAsync(JobContext,
                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true)
                    .Callback<IPipelineJobContext, CancellationToken>((p, c) =>
                        mockJct.Setup(jct => jct.ThrowIfCancellationRequested())
                            .Throws(new OperationCanceledException()));
            var pipelineServer = GetPipelineServer();

            // Act
            Assert.Throws<OperationCanceledException>(() =>
                pipelineServer.ExecuteJob(JobId, mockJct.Object));
        }

        [Test]
        public void Dispose_DisposesPipelineStorage()
        {
            // Arrange
            var pipelineServer = GetPipelineServer();

            // Act
            pipelineServer.Dispose();

            // Assert
            Assert.DoesNotThrow(() => MockStorage.Verify(storage => storage.Dispose()));
        }
    }
}