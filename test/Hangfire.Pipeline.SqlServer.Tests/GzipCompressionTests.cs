using NUnit.Framework;
using System.IO;

namespace Hangfire.Pipeline.SqlServer.Tests
{
    [TestFixture]
    public class GzipCompressionTests
    {
        protected GzipCompression GetGzipCompression()
        {
            return new GzipCompression();
        }

        [Test]
        public void CompressBytes_ReturnsCompressedByteArray()
        {
            // Arrange
            var gzip = GetGzipCompression();
            var bytes = new byte[] { 0, 1, 2, 3, 4, 5 };

            // Act
            var compressed = gzip.CompressBytes(bytes);

            // Assert
            Assert.IsInstanceOf<byte[]>(compressed);
            Assert.AreNotEqual(bytes, compressed);
        }

        [Test]
        public void DecompressBytes_ReturnsCompressedByteArray()
        {
            // Arrange
            var gzip = GetGzipCompression();
            var bytes = new byte[] { 0, 1, 2, 3, 4, 5 };
            var compressed = gzip.CompressBytes(bytes);

            // Act
            var decompressed = gzip.DecompressBytes(compressed);

            // Assert
            Assert.AreEqual(bytes, decompressed);
        }

        [Test]
        public void DecompressBytes_NonGzipInputThrows()
        {
            // Arrange
            var gzip = GetGzipCompression();
            var nonGzipBytes = new byte[] { 0, 1, 2, 3, 4, 5 };

            // Act/Assert
            Assert.Throws<InvalidDataException>(() => gzip.DecompressBytes(nonGzipBytes));
        }
    }
}
