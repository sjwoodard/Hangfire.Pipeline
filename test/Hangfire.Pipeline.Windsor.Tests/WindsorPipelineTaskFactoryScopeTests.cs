using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Moq;
using NUnit.Framework;
using System;

namespace Hangfire.Pipeline.Windsor.Tests
{
    [TestFixture]
    public class WindsorPipelineTaskFactoryScopeTests
    {
        [Test]
        public void Constructor_ContainerIsNull_Throw()
        {
            // Arrange/Act/Assert
            Assert.Throws<ArgumentNullException>(() =>
                new WindsorPipelineTaskFactoryScope(null));
        }

        [Test]
        public void Constructor_CreatesNewWindsorContainerScope()
        {
            // Arrange
            var container = new WindsorContainer();
            container.Register(Component.For<TestScopedObject>().LifestyleScoped());

            // Act
            var scope = new WindsorPipelineTaskFactoryScope(container);
            var scoped1 = container.Resolve<TestScopedObject>();
            var scoped2 = container.Resolve<TestScopedObject>();
            scope.Dispose();

            // Assert
            Assert.AreEqual(scoped1, scoped2);
            Assert.Throws<InvalidOperationException>(() =>
                container.Resolve<TestScopedObject>());
        }

        public class TestScopedObject
        {
        }
    }
}