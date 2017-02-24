using Castle.Windsor;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.Windsor.Tests
{
    [TestFixture]
    public class WindsorPipelineTaskFactoryTests
    {
        [Test]
        public void Constructor_ContainerIsNull_Throw()
        {
            // Arrange/Act/Assert
            Assert.Throws<ArgumentNullException>(() => new WindsorPipelineTaskFactory(null));
        }

        [Test]
        public void Create_ResolvesPipelineTaskByName()
        {
            // Arrange
            var taskName = "task";
            var mockContainer = new Mock<IWindsorContainer>();
            mockContainer.Setup(container => container.Resolve<IPipelineTask>(taskName))
                .Returns(new TestPipelineTask())
                .Verifiable();
            var factory = new WindsorPipelineTaskFactory(mockContainer.Object);

            // Act
            var task = factory.Create(taskName);

            // Assert
            Assert.DoesNotThrow(() => mockContainer.VerifyAll());
            Assert.IsInstanceOf<TestPipelineTask>(task);
        }

        [Test]
        public void Release_ReleasesTaskInstance()
        {
            // Assert
            var mockContainer = new Mock<IWindsorContainer>();
            var mockTask = new Mock<IPipelineTask>();
            var factory = new WindsorPipelineTaskFactory(mockContainer.Object);

            // Act
            factory.Release(mockTask.Object);

            // Assert
            Assert.DoesNotThrow(() => mockContainer.Verify(container =>
                container.Release(mockTask.Object)));
        }

        [Test]
        public void Release_IfTaskInstanceIsNullDoNothing()
        {
            // Assert
            var mockContainer = new Mock<IWindsorContainer>();
            var factory = new WindsorPipelineTaskFactory(mockContainer.Object);

            // Act
            factory.Release(null);

            // Assert
            Assert.That(() => mockContainer.Verify(container =>
                container.Release(It.IsAny<object>())), Throws.Exception);
        }

        [Test]
        public void GetScope_ReturnsWindsorPipelineScope()
        {
            // Arrange
            var mockContainer = new Mock<IWindsorContainer>();
            var factory = new WindsorPipelineTaskFactory(mockContainer.Object);

            // Act
            var scope = factory.GetScope();

            // Assert
            Assert.IsInstanceOf<WindsorPipelineTaskFactoryScope>(scope);
        }

        public class TestPipelineTask : IPipelineTask
        {
            public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }
        }
    }
}
