using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.Pipeline.Tests
{
    [TestFixture]
    public class PipelineExtensionsTests
    {
        [Test]
        public void QueueTask_IfQueueIsNull_CreateNewQueueAndQueueTask()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            var taskContext = new PipelineTaskContext();

            // Act
            jobContext.QueueTask(taskContext);

            // Assert
            Assert.AreEqual(taskContext, jobContext.Queue.Single());
        }

        [Test]
        public void QueueTask_IfQueueAlreadyExists_AppendTaskToQueue()
        {
            // Arrange
            var taskContext1 = new PipelineTaskContext { Id = "1" };
            var taskContext2 = new PipelineTaskContext { Id = "2" };
            var jobContext = new PipelineJobContext();
            jobContext.Queue = new[] { taskContext1 };

            // Act
            jobContext.QueueTask(taskContext2);

            // Assert
            Assert.AreEqual(taskContext1, jobContext.Queue.ElementAt(0));
            Assert.AreEqual(taskContext2, jobContext.Queue.ElementAt(1));
        }

        [Test]
        public void QueueTask_IfEnvironmentIsNull_CreateDictionaryAndAddVariable()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            var taskContext = new PipelineTaskContext();

            // Act
            jobContext.AddEnvironment("key", "value");

            // Assert
            Assert.AreEqual("value", jobContext.Environment["key"]);
        }

        [Test]
        public void AddEnvironment_IfEnvironmentAlreadyExists_AddVariableToDictionary()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            jobContext.Environment = new Dictionary<string, object>();
            jobContext.Environment["key1"] = "value1";

            // Act
            jobContext.AddEnvironment("key2", "value2");

            // Assert
            Assert.AreEqual("value1", jobContext.Environment["key1"]);
            Assert.AreEqual("value2", jobContext.Environment["key2"]);
        }

        [Test]
        public void GetEnvironment_IfEnvironmentExists_ReturnEnvironmentCastAsRequestedType()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            var expectedInt = 1;
            var expectedString = "foo";
            jobContext.AddEnvironment("int", expectedInt);
            jobContext.AddEnvironment("string", expectedString);

            // Act
            var intResult = jobContext.GetEnvironment<int>("int");
            var stringResult = jobContext.GetEnvironment<string>("string");

            // Assert
            Assert.AreEqual(expectedInt, intResult);
            Assert.AreEqual(expectedString, stringResult);
        }

        [Test]
        public void GetEnvironment_IfEnvironmentObjectDoesNotExists_Throw()
        {
            // Arrange
            var jobContext = new PipelineJobContext();

            // Act/Assert
            Assert.Throws<NullReferenceException>(() => jobContext.GetEnvironment<int>("key"));
        }

        [Test]
        public void GetEnvironment_IfEnvironmentKeyNotExists_Throw()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            jobContext.AddEnvironment("key", "value");

            // Act/Assert
            Assert.Throws<KeyNotFoundException>(() => jobContext.GetEnvironment<int>("notexists"));
        }

        [Test]
        public void AddResult_IfResultsAreNull_CreateDictionaryAndAddResult()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            var taskContext = new PipelineTaskContext();

            // Act
            jobContext.AddResult("key", "value");

            // Assert
            Assert.AreEqual("value", jobContext.Result["key"]);
        }

        [Test]
        public void AddResult_IfResultsAlreadyExists_AddResultToDictionary()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            jobContext.Result = new Dictionary<string, object>();
            jobContext.Result["key1"] = "value1";

            // Act
            jobContext.AddResult("key2", "value2");

            // Assert
            Assert.AreEqual("value1", jobContext.Result["key1"]);
            Assert.AreEqual("value2", jobContext.Result["key2"]);
        }

        [Test]
        public void GetResult_IfResultExists_ReturnResultCastAsRequestedType()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            var expectedInt = 1;
            var expectedString = "foo";
            jobContext.AddResult("int", expectedInt);
            jobContext.AddResult("string", expectedString);

            // Act
            var intResult = jobContext.GetResult<int>("int");
            var stringResult = jobContext.GetResult<string>("string");

            // Assert
            Assert.AreEqual(expectedInt, intResult);
            Assert.AreEqual(expectedString, stringResult);
        }

        [Test]
        public void GetResult_IfResultObjectDoesNotExists_Throw()
        {
            // Arrange
            var jobContext = new PipelineJobContext();

            // Act/Assert
            Assert.Throws<NullReferenceException>(() => jobContext.GetResult<int>("key"));
        }

        [Test]
        public void GetResult_IfResultKeyNotExists_Throw()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            jobContext.AddResult("key", "value");

            // Act/Assert
            Assert.Throws<KeyNotFoundException>(() => jobContext.GetResult<int>("notexists"));
        }

        [Test]
        public void AddCompletedTask_IfCompletedObjectIsNull_CreateNewCompletedArrayAndQueueTask()
        {
            // Arrange
            var jobContext = new PipelineJobContext();
            var taskContext = new PipelineTaskContext();

            // Act
            jobContext.AddCompletedTask(taskContext);

            // Assert
            Assert.AreEqual(taskContext, jobContext.Completed.Single());
        }

        [Test]
        public void AddCompletedTask_IfCompletedAlreadyExists_AppendTaskToCompleted()
        {
            // Arrange
            var taskContext1 = new PipelineTaskContext { Id = "1" };
            var taskContext2 = new PipelineTaskContext { Id = "2" };
            var jobContext = new PipelineJobContext();
            jobContext.Completed = new[] { taskContext1 };

            // Act
            jobContext.AddCompletedTask(taskContext2);

            // Assert
            Assert.AreEqual(taskContext1, jobContext.Completed.ElementAt(0));
            Assert.AreEqual(taskContext2, jobContext.Completed.ElementAt(1));
        }

        [Test]
        public void SetArg_IfArgsDictionaryIsNull_CreateNewDictionaryAndAddArg()
        {
            // Arrange
            var taskContext = new PipelineTaskContext();

            // Act
            taskContext.AddArg("key", "value");

            // Arrange
            Assert.AreEqual("value", taskContext.Args["key"]);
        }

        [Test]
        public void AddArg_IfArgsDictionaryIsNull_CreateNewDictionaryAndAddArg()
        {
            // Arrange
            var taskContext = new PipelineTaskContext();

            // Act
            taskContext.AddArg("key", "value");

            // Arrange
            Assert.AreEqual("value", taskContext.Args["key"]);
        }

        [Test]
        public void GetArg_ConvertIConvertableTypes()
        {
            // Arrange
            var taskContext = new PipelineTaskContext();

            // Act
            taskContext.AddArg("object", new object());
            taskContext.AddArg("string", "string");
            taskContext.AddArg("int", 1);
            taskContext.AddArg("bool", true);
            taskContext.AddArg("date", DateTime.UtcNow);

            // Arrange
            Assert.IsInstanceOf<object>(taskContext.GetArg<object>("object"));
            Assert.IsInstanceOf<string>(taskContext.GetArg<string>("string"));
            Assert.IsInstanceOf<int>(taskContext.GetArg<int>("int"));
            Assert.IsInstanceOf<bool>(taskContext.GetArg<bool>("bool"));
            Assert.IsInstanceOf<DateTime>(taskContext.GetArg<DateTime>("date"));
        }

        [Test]
        public void GetArg_IfArgsObjectDoesNotExists_Throw()
        {
            // Arrange
            var taskContext = new PipelineTaskContext();

            // Act/Assert
            Assert.Throws<NullReferenceException>(() => taskContext.GetArg<int>("key"));
        }

        [Test]
        public void GetArg_IfTaskKeyNotExists_Throw()
        {
            // Arrange
            var taskContext = new PipelineTaskContext();
            taskContext.AddArg("key", "value");

            // Act/Assert
            Assert.Throws<KeyNotFoundException>(() => taskContext.GetArg<int>("notexists"));
        }

        [Test]
        public void ToObject_CastDictionaryToObject()
        {
            // Arrange
            var dict = new Dictionary<string, object>();
            dict.Add("Id", 1);
            dict.Add("Name", "foo");

            // Act
            var result = dict.ToObject<ToObjectTest>();

            // Assert
            Assert.AreEqual(1, result.Id);
            Assert.AreEqual("foo", result.Name);
        }

        [Test]
        public void ToObject_DoesNotThrowIfDictionaryContainsPropertyThatDoesNotExist()
        {
            // Arrange
            var dict = new Dictionary<string, object>();
            dict.Add("Id", 1);
            dict.Add("NonExistantProperty", "foo");

            // Act
            var result = dict.ToObject<ToObjectTest>();

            // Assert
            Assert.AreEqual(1, result.Id);
            Assert.IsNull(result.Name);
        }

        private class ToObjectTest
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}