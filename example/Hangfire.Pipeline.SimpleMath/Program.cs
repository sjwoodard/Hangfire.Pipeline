using Hangfire.Logging;
using Hangfire.Pipeline.SqlServer;
using Hangfire.SqlServer;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Hangfire.Pipeline.SimpleMath
{
    /// <summary>
    /// This example uses SQL Server for job storage and Castle Windsor for dependency injection. You
    /// need to configure your SQL Server information at the top of this class.
    /// </summary>
    public class Program
    {
        // Setup your data connection
        private const string SqlConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private const string SqlDataTableName = "SampleDb.dbo.MyDataTable";
        private const string SqlDataPrimaryKeyColumn = "Id";
        private const string SqlDataValueColumn = "Data";

        private static readonly ILog Log = LogProvider.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            // Start logging
            var logConfigFileInfo = new FileInfo("log.config");
            XmlConfigurator.Configure(logConfigFileInfo);

            // Build pipeline SQL Server storage
            Log.Info("Build pipeline SQL server storage");
            var pipelineStorageOptions = new SqlPipelineStorageOptions();
            pipelineStorageOptions.ConnectionFactory = new SqlConnectionFactory(SqlConnectionString);
            pipelineStorageOptions.Table = SqlDataTableName;
            pipelineStorageOptions.KeyColumn = SqlDataPrimaryKeyColumn;
            pipelineStorageOptions.ValueColumn = SqlDataValueColumn;
            var pipelineStorage = new SqlPipelineStorage(pipelineStorageOptions);

            // Build pipeline server
            Log.Info("Build pipeline server");
            var pipelineServer = new PipelineServer(pipelineStorage);

            // Build a Hangfire server that uses the pipeline server to execute jobs
            Log.Info("Configure Hangfire");
            JobActivator.Current = new PipelineJobActivator(pipelineServer);
            JobStorage.Current = new SqlServerStorage(SqlConnectionString);
            var hangfireServer = new BackgroundJobServer();

            // Build a Hangfire job client using SQL Storage
            Log.Info("Building Hangfire clients");
            var hangfireClient = new BackgroundJobClient();

            // Create a pipeline client that wraps the Hangfire job clients
            Log.Info("Building pipeline client");
            var client = new PipelineClient(pipelineStorage, hangfireClient);

            // Build job and tasks
            var jobContext = new PipelineJobContext();
            jobContext.Id = Guid.NewGuid().ToString();
            jobContext.QueueTask(new PipelineTaskContext()
            {
                Task = "Hangfire.Pipeline.SimpleMath.DoubleValueTask",
                Id = "double-1",
                RunParallel = false,
                Args = new Dictionary<string, object> { { "value", 2 } }
            });
            jobContext.QueueTask(new PipelineTaskContext()
            {
                Task = "Hangfire.Pipeline.SimpleMath.DelayTask",
                Id = "delay-1",
                RunParallel = false,
                Args = new Dictionary<string, object> { { "delay", 1000 } }
            });
            jobContext.QueueTask(new PipelineTaskContext()
            {
                Task = "Hangfire.Pipeline.SimpleMath.SquareRootTask",
                Id = "squareRoot-1",
                RunParallel = false,
                Args = new Dictionary<string, object> { { "value", 2 } }
            });

            // Store job
            client.Storage.CreateJobContextAsync(jobContext, CancellationToken.None).Wait();

            // Submit job
            var enqueuedJobContext = client.EnqueueAsync(jobContext).Result;
            Log.InfoFormat("Enqueued job with Hangfire ID '{0}'", enqueuedJobContext.HangfireId);

            // Wait for key press then shutdown
            Console.ReadKey();
            hangfireServer.Dispose();
            pipelineServer.Dispose();
        }
    }
}
