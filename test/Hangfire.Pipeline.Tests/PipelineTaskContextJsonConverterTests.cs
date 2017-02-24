using NUnit.Framework;
using System;

namespace Hangfire.Pipeline.Tests
{
    [TestFixture]
    public class PipelineTaskContextJsonConverterTests
    {
        [TestCase(typeof(string))]
        [TestCase(typeof(PipelineTaskContextJsonConverterTests))]
        public void Create_ReturnsInstanceOfIPipelineTaskContextForAnyType(Type type)
        {
            // Arrange
            var converter = new PipelineTaskContextJsonConverter();

            // Act
            var res = converter.Create(type);

            // Assert
            Assert.IsInstanceOf<IPipelineTaskContext>(res);
        }
    }
}
