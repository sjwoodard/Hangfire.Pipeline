using Moq;
using NUnit.Framework;
using System;
using System.Data.SqlClient;
using System.Security;

namespace Hangfire.Pipeline.SqlServer.Tests
{
    [TestFixture]
    public class SqlConnectionFactoryTests
    {
        private const string ConnectionString = "Server=myServerAddress;Database=myDataBase;";

        [Test]
        public void Constructor_SqlConnectionStringIsNull_Throw()
        {
            // Arrange/Act/Assert
            Assert.Throws<ArgumentNullException>(() => new SqlConnectionFactory(null));
        }

        [Test]
        public void Create_ReturnsNewSqlConnetion()
        {
            // Arrange
            var factory = new SqlConnectionFactory(ConnectionString);

            // Act
            var conn = factory.Create();

            // Assert
            Assert.IsInstanceOf<SqlConnectionWrapper>(conn);
        }

        [Test]
        public void Create_WithCredentials_ReturnsNewSqlConnetion()
        {
            // Arrange
            var password = "password";
            var securePassword = new SecureString();
            foreach (char c in password)
                securePassword.AppendChar(c);
            securePassword.MakeReadOnly();
            var credential = new SqlCredential("userid", securePassword);
            var factory = new SqlConnectionFactory(ConnectionString, credential);

            // Act
            var conn = factory.Create();

            // Assert
            Assert.IsInstanceOf<SqlConnectionWrapper>(conn);
        }

        [Test]
        public void Release_DisposesSqlConnection()
        {
            // Arrange
            var factory = new SqlConnectionFactory(ConnectionString);
            var mockSqlConnectionWrapper = new Mock<ISqlConnectionWrapper>();

            // Act
            factory.Release(mockSqlConnectionWrapper.Object);

            // Assert
            Assert.DoesNotThrow(() =>
                mockSqlConnectionWrapper.Verify(conn => conn.Dispose()));
        }

        [Test]
        public void Release_SqlConnectionIsNull_DoesNotThrow()
        {
            // Arrange
            var factory = new SqlConnectionFactory(ConnectionString);

            // Act/Assert
            Assert.DoesNotThrow(() => factory.Release(null));
        }
    }
}
