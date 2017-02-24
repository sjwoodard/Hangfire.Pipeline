using Moq;
using NUnit.Framework;
using System;

namespace Hangfire.Pipeline.Tests
{
    [TestFixture]
    public class PipelineJobActivatorTests
    {
        [TestCase(typeof(object))]
        [TestCase(typeof(int))]
        [TestCase(typeof(PipelineJobActivatorTests))]
        public void ActivateJob_GivenAnyType_ReturnsInstanceOfPipelineServer(Type type)
        {
            // Arrange
            var mockPipelineServer = new Mock<IPipelineServer>();
            var activator = new PipelineJobActivator(mockPipelineServer.Object);

            // Act
            var activated = activator.ActivateJob(type);

            // Assert
            Assert.AreEqual(mockPipelineServer.Object, activated);
        }

        [TestCase(typeof(object))]
        [TestCase(typeof(int))]
        [TestCase(typeof(PipelineJobActivatorTests))]
        public void ActivateJobAcoped_GivenAnyType_ReturnsInstanceOfPipelineServer(Type type)
        {
            // Arrange
            var mockPipelineServer = new Mock<IPipelineServer>();
            var activator = new PipelineJobActivator(mockPipelineServer.Object);

            // Act
            var scoped = activator.BeginScope();
            var activated = scoped.Resolve(type);

            // Assert
            Assert.AreEqual(mockPipelineServer.Object, activated);
        }
    }
}