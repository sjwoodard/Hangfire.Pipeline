using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.SqlServer.Tests
{
    [TestFixture]
    public class SqlPipelineStorageTests
    {
        private const string ConnectionString = "Server=myServerAddress;Database=myDataBase;";
        private const string Table = "table";
        private const string KeyColumn = "key";
        private const string ValueColumn = "value";

        private CancellationToken Ct;
        private Mock<IPipelineSerializer> MockSerializer;
        private Mock<ISqlPipelineStorageCompression> MockCompression;
        private Mock<ISqlConnectionFactory> MockConnectionFactory;
        private Mock<ISqlConnectionWrapper> MockSqlConnectionWrapper;
        private Mock<ISqlCommandWrapper> MockSqlCommandWrapper;
        private IDictionary<string, object> ParameterCallbacks;
        private string CommandTextCallback => MockSqlCommandWrapper.Object.CommandText;
        private bool UseCompression;

        [SetUp]
        public void Setup()
        {
            Ct = new CancellationToken();
            MockSerializer = new Mock<IPipelineSerializer>();
            MockCompression = new Mock<ISqlPipelineStorageCompression>();
            MockConnectionFactory = new Mock<ISqlConnectionFactory>();
            MockSqlConnectionWrapper = new Mock<ISqlConnectionWrapper>();
            MockSqlCommandWrapper = new Mock<ISqlCommandWrapper>();
            ParameterCallbacks = new Dictionary<string, object>();
            UseCompression = true;
        }

        protected SqlPipelineStorage GetSqlPipelineStorage()
        {
            var options = new SqlPipelineStorageOptions();
            options.Compression = MockCompression.Object;
            options.UseCompression = UseCompression;
            options.ConnectionFactory = MockConnectionFactory.Object;
            options.Serializer = MockSerializer.Object;
            options.Table = Table;
            options.KeyColumn = KeyColumn;
            options.ValueColumn = ValueColumn;
            MockSqlCommandWrapper.SetupProperty(cmd => cmd.CommandText);
            MockSqlCommandWrapper.Setup(cmd => cmd.AddParameterWithValue(
                It.IsAny<string>(), It.IsAny<object>()))
                    .Callback<string, object>((key, val) => ParameterCallbacks.Add(key, val));
            MockSqlConnectionWrapper.Setup(conn => conn.OpenAsync(Ct))
                .Returns(Task.FromResult(0))
                .Verifiable();
            MockSqlConnectionWrapper.Setup(conn => conn.CreateCommand())
                .Returns(MockSqlCommandWrapper.Object)
                .Verifiable();
            MockSqlConnectionWrapper.Setup(conn => conn.Close())
                .Verifiable();
            MockConnectionFactory.Setup(factory => factory.Create())
                .Returns(MockSqlConnectionWrapper.Object)
                .Verifiable();
            var storage = new SqlPipelineStorage(options);
            return storage;
        }

        [Test]
        public void Constructor_OptionsAreNull_Throw()
        {
            // Arrange/Act/Assert
            Assert.Throws<ArgumentNullException>(() => new SqlPipelineStorage(null));
        }

        [TestCase(0, false)]
        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(1, true)]
        public async Task CreateJobContextAsync_SerializesAndCompressesJobContextThenInsertsSqlServerRecord_ReturnsTrueIfRowsAffected(int rowsAffected, bool useCompression)
        {
            // Arrange
            var commandText = $"insert into {Table} ({KeyColumn},{ValueColumn}) values (@key,@value)";
            UseCompression = useCompression;
            var jobContext = new PipelineJobContext { Id = "1" };
            var serialized = "serialized";
            var bytes = Encoding.UTF8.GetBytes(serialized);
            var expectedBytes = useCompression ? Encoding.UTF8.GetBytes("compressed") : bytes;
            MockSerializer.Setup(ser => ser.Serialize(jobContext))
                .Returns(serialized)
                .Verifiable();
            if (useCompression)
            {
                MockCompression.Setup(comp => comp.CompressBytes(bytes))
                    .Returns(expectedBytes)
                    .Verifiable();
            }
            MockSqlCommandWrapper.Setup(cmd => cmd.ExecuteNonQueryAsync(Ct))
                .ReturnsAsync(rowsAffected)
                .Verifiable();
            var storage = GetSqlPipelineStorage();

            // Act
            var res = await storage.CreateJobContextAsync(jobContext, Ct);

            // Assert
            Assert.DoesNotThrow(() => MockCompression.VerifyAll());
            Assert.DoesNotThrow(() => MockSerializer.VerifyAll());
            Assert.DoesNotThrow(() => MockSqlConnectionWrapper.VerifyAll());
            Assert.DoesNotThrow(() => MockSqlCommandWrapper.Verify(cmd =>
                cmd.ExecuteNonQueryAsync(Ct)));
            Assert.AreEqual(commandText, CommandTextCallback);
            Assert.AreEqual(rowsAffected > 0, res);
            Assert.AreEqual(jobContext.Id, ParameterCallbacks["key"]);
            Assert.AreEqual(expectedBytes, ParameterCallbacks["value"]);
        }

        [TestCase(0)]
        [TestCase(1)]
        public async Task DeleteJobContextAsync_DeletesSqlServerRecord_ReturnsTrueIfRowsAffected(int rowsAffected)
        {
            // Arrange
            var commandText = $"delete from {Table} where {KeyColumn}=@key";
            var id = "1";
            MockSqlCommandWrapper.Setup(cmd => cmd.ExecuteNonQueryAsync(Ct))
                .ReturnsAsync(rowsAffected)
                .Verifiable();
            var storage = GetSqlPipelineStorage();

            // Act
            var res = await storage.DeleteJobContextAsync(id, Ct);

            // Assert
            Assert.DoesNotThrow(() => MockSqlConnectionWrapper.VerifyAll());
            Assert.DoesNotThrow(() => MockSqlCommandWrapper.Verify(cmd =>
                cmd.ExecuteNonQueryAsync(Ct)));
            Assert.AreEqual(commandText, CommandTextCallback);
            Assert.AreEqual(rowsAffected > 0, res);
            Assert.AreEqual(id, ParameterCallbacks["key"]);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task GetJobContextAsync_SelectsOneFromSqlServerDecompressesAndDeserializesIPipelineJobContext(bool useCompression)
        {
            // Arrange
            var commandText = $"select top 1 {ValueColumn} from {Table} where {KeyColumn}=@key";
            UseCompression = useCompression;
            var jobContext = new PipelineJobContext { Id = "1" };
            var decompressed = "decompressed";
            var bytes = Encoding.UTF8.GetBytes(decompressed);
            var expectedBytes = useCompression ? Encoding.UTF8.GetBytes("compressed") : bytes;
                MockSqlCommandWrapper.Setup(cmd => cmd.ExecuteScalarAsync(Ct))
                    .ReturnsAsync(expectedBytes)
                    .Verifiable();
            if (useCompression)
            {
                MockCompression.Setup(comp => comp.DecompressBytes(expectedBytes))
                    .Returns(bytes)
                    .Verifiable();
            }
            MockSerializer.Setup(ser => ser.Deserialize(decompressed))
                .Returns(jobContext)
                .Verifiable();
            var storage = GetSqlPipelineStorage();

            // Act
            var res = await storage.GetJobContextAsync(jobContext.Id, Ct);

            // Assert
            Assert.DoesNotThrow(() => MockSqlConnectionWrapper.VerifyAll());
            Assert.DoesNotThrow(() => MockSqlCommandWrapper.Verify(cmd =>
                cmd.ExecuteScalarAsync(Ct)));
            Assert.DoesNotThrow(() => MockCompression.VerifyAll());
            Assert.DoesNotThrow(() => MockSerializer.VerifyAll());
            Assert.AreEqual(commandText, CommandTextCallback);
            Assert.AreEqual(jobContext.Id, ParameterCallbacks["key"]);
        }

        [TestCase(0, false)]
        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(1, true)]
        public async Task UpdateJobContextAsync_SerializesAndCompressesJobContextThenInsertsSqlServerRecord_ReturnsTrueIfRowsAffected(int rowsAffected, bool useCompression)
        {
            // Arrange
            var commandText = $"update {Table} set {ValueColumn}=@value where {KeyColumn}=@key";
            UseCompression = useCompression;
            var jobContext = new PipelineJobContext { Id = "1" };
            var serialized = "serialized";
            var bytes = Encoding.UTF8.GetBytes(serialized);
            var expectedBytes = useCompression ? Encoding.UTF8.GetBytes("compressed") : bytes;
            MockSerializer.Setup(ser => ser.Serialize(jobContext))
                .Returns(serialized)
                .Verifiable();
            if (useCompression)
            {
                MockCompression.Setup(comp => comp.CompressBytes(bytes))
                    .Returns(expectedBytes)
                    .Verifiable();
            }
            MockSqlCommandWrapper.Setup(cmd => cmd.ExecuteNonQueryAsync(Ct))
                .ReturnsAsync(rowsAffected)
                .Verifiable();
            var storage = GetSqlPipelineStorage();

            // Act
            var res = await storage.UpdateJobContextAsync(jobContext, Ct);

            // Assert
            Assert.DoesNotThrow(() => MockCompression.VerifyAll());
            Assert.DoesNotThrow(() => MockSerializer.VerifyAll());
            Assert.DoesNotThrow(() => MockSqlConnectionWrapper.VerifyAll());
            Assert.DoesNotThrow(() => MockSqlCommandWrapper.Verify(cmd =>
                cmd.ExecuteNonQueryAsync(Ct)));
            Assert.AreEqual(commandText, CommandTextCallback);
            Assert.AreEqual(rowsAffected > 0, res);
            Assert.AreEqual(jobContext.Id, ParameterCallbacks["key"]);
            Assert.AreEqual(expectedBytes, ParameterCallbacks["value"]);
        }
    }
}
