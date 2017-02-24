using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using System;

namespace Hangfire.Pipeline.Tests
{
    [TestFixture]
    public class JsonPipelineSerializerTests
    {
        [Test]
        public void Serialize_SerializesJobContextToJson()
        {
            // Arrange
            var jobContext = new PipelineJobContext { Id = "1" };
            var expected = JsonConvert.SerializeObject(jobContext);
            var serializer = new JsonPipelineSerializer();

            // Act
            var res = serializer.Serialize(jobContext);

            // Assert
            Assert.AreEqual(expected, res);
        }

        [Test]
        public void Serialize_SerializesJobContextToJsonWithSerializerSettings()
        {
            // Arrange
            var jobContext = new PipelineJobContext { Id = "1" };
            var settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            var expected = JsonConvert.SerializeObject(jobContext, settings);
            var serializer = new JsonPipelineSerializer(typeof(PipelineJobContext), settings);

            // Act
            var actual = serializer.Serialize(jobContext);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Deserialize_DeserializesJsonToJobContext()
        {
            // Arrange
            var jobContext = new PipelineJobContext { Id = "1" };
            var serialized = JsonConvert.SerializeObject(jobContext);
            var serializer = new JsonPipelineSerializer();

            // Act
            var res = serializer.Deserialize(serialized);

            // Assert
            Assert.AreEqual(jobContext.Id, res.Id);
        }

        [Test]
        public void Deserialize_DeserializesJobContextToJsonWithSerializerSettings()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            jobContext.Queue = new[] { new PipelineTaskContext() };
            var serialized = JsonConvert.SerializeObject(jobContext);
            var settings = new JsonSerializerSettings();
            settings.Converters = new[] { new ThrowsConverter() };
            var serializer = new JsonPipelineSerializer(typeof(PipelineJobContext), settings);

            // Act/Assert
            Assert.Throws<NotImplementedException>(() => serializer.Deserialize(serialized));
        }

        public class ThrowsConverter : CustomCreationConverter<IPipelineTaskContext>
        {
            public override IPipelineTaskContext Create(Type objectType)
            {
                throw new NotImplementedException();
            }
        }
    }
}