using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.Tests
{
    [TestFixture]
    public class ReflectionPipelineTaskFactoryTests
    {
        [Test]
        public void Constructor_NullAssembly_Throw()
        {
            // Arrange/Act/Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ReflectionPipelineTaskFactory(null));
        }

        [Test]
        public void Create_CreateInstanceOfTypeFromAssembly()
        {
            // Arrange
            var type = typeof(TestPipelineTask);
            var factory = new ReflectionPipelineTaskFactory(type.Assembly);

            // Act
            var task = factory.Create(type.FullName);

            // Assert
            Assert.IsInstanceOf<IPipelineTask>(task);
        }

        [Test]
        public void Create_TypeDoesNotExistInAssembly_Throw()
        {
            // Arrange
            var type = typeof(TestPipelineTask);
            var factory = new ReflectionPipelineTaskFactory(type.Assembly);

            // Act/Assert
            Assert.Throws<NullReferenceException>(() => factory.Create("foo"));
        }

        [Test]
        public void Create_TypeIsNotIPipelineTask_Throw()
        {
            // Arrange
            var type = typeof(NotPipelineTask);
            var factory = new ReflectionPipelineTaskFactory(type.Assembly);

            // Act/Assert
            Assert.Throws<InvalidCastException>(() => factory.Create(type.FullName));
        }

        [Test]
        public void Release_DisposesPipelineTask()
        {
            // Arrange
            var type = typeof(TestPipelineTask);
            var factory = new ReflectionPipelineTaskFactory(type.Assembly);
            var mockTask = new Mock<IPipelineTask>();
            mockTask.Setup(task => task.Dispose()).Verifiable();

            // Act
            factory.Release(mockTask.Object);

            // Assert
            Assert.DoesNotThrow(() => mockTask.VerifyAll());
        }

        [Test]
        public void Release_NullPipelineTask_DoesNotThrow()
        {
            // Arrange
            var type = typeof(TestPipelineTask);
            var factory = new ReflectionPipelineTaskFactory(type.Assembly);

            // Act/Assert
            Assert.DoesNotThrow(() => factory.Release(null));
        }

        [Test]
        public void GetScope_ReturnsNull()
        {
            // Arrange
            var type = typeof(TestPipelineTask);
            var factory = new ReflectionPipelineTaskFactory(type.Assembly);

            // Act
            var getScope = factory.GetScope();

            // Assert
            Assert.IsNull(getScope);
        }

        public class NotPipelineTask
        {
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