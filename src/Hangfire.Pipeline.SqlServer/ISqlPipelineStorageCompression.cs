namespace Hangfire.Pipeline.SqlServer
{
    public interface ISqlPipelineStorageCompression
    {
        byte[] CompressBytes(byte[] bytes);
        byte[] DecompressBytes(byte[] bytes);
    }
}