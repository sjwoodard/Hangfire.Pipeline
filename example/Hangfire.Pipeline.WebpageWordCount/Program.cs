using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Hangfire.Logging;
using Hangfire.Pipeline.WebpageWordCount.Tasks;
using Hangfire.Pipeline.SqlServer;
using Hangfire.Pipeline.Windsor;
using Hangfire.SqlServer;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Hangfire.Pipeline.WebpageWordCount
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
        private static IPipelineServer _pipelineServer;
        private static BackgroundJobServer _hangfireServer;

        public static void Main(string[] args)
        {
            // Start logging
            var logConfigFileInfo = new FileInfo("log.config");
            XmlConfigurator.Configure(logConfigFileInfo);
            // Get a new cancellation token
            var cts = new CancellationTokenSource();
            var ct = cts.Token;
            try
            {
                // Get the pipeline SQL Server storage connection
                var pipelineStorage = GetPipelineStorage();

                // Get Hangfire storage connection
                var hangfireStorage = new SqlServerStorage(SqlConnectionString);

                // Start the pipeline server, this manages pipeline jobs that will run on Hangfire
                StartServer(pipelineStorage, hangfireStorage);

                // Get a pipeline client, this is a wrapper over the Hangfire job client
                var client = GetClient(pipelineStorage, hangfireStorage);

                // Create a new pipeline job
                var jobContext = new PipelineJobContext();
                jobContext.Id = Guid.NewGuid().ToString();

                // Add URLs to our environment
                var urls = new[] {
                    "http://www.nyse.com",
                    "http://www.cnn.com",
                    "http://www.att.com",
                    "http://www.ibm.com",
                    "http://www.ford.com",
                    "http://www.vizio.com",
                    "http://www.apache.org",
                    "http://www.ge.com"
                };
                jobContext.AddEnvironment("urls", string.Join(",", urls));

                // Create some tasks to run in the job

                // The first task will use an HTTP client to retrieve the websites
                jobContext.QueueTask(new PipelineTaskContext()
                {
                    Task = "GetWebpage",
                    Id = Guid.NewGuid().ToString(),
                    RunParallel = true,
                    Priority = 100
                });

                // The second task will strip all the HTML tags
                jobContext.QueueTask(new PipelineTaskContext()
                {
                    Task = "GetWebpageText",
                    Id = Guid.NewGuid().ToString(),
                    RunParallel = false,
                    Priority = 200
                });

                // The next task will tokenize the text and count the number of tokens
                jobContext.QueueTask(new PipelineTaskContext()
                {
                    Task = "CountWords",
                    Id = Guid.NewGuid().ToString(),
                    RunParallel = false,
                    Priority = 300,
                    Args = new Dictionary<string, object> { { "pattern", @"\w+" } }
                });

                // The last task will log the results
                jobContext.QueueTask(new PipelineTaskContext()
                {
                    Task = "LogResult",
                    Id = Guid.NewGuid().ToString(),
                    RunParallel = false,
                    Priority = 400
                });

                // Store the job in the pipeline SQL Server storage
                client.Storage.CreateJobContextAsync(jobContext, ct).Wait();

                // Execute the job in Hangfire
                var enqueuedJobContext = client.EnqueueAsync(jobContext).Result;
                Log.InfoFormat("Enqueued job with Hangfire ID '{0}'", enqueuedJobContext.HangfireId);
            }
            catch (Exception ex)
            {
                Log.ErrorException(ex.Message, ex);
            }
            // Wait for a key press
            Console.ReadKey();

            // Shutdown
            CloseServer();
            cts.Cancel();
        }

        public static IPipelineStorage GetPipelineStorage()
        {
            Log.Info("Building pipeline storage");

            // Set connection information
            var pipelineStorageOptions = new SqlPipelineStorageOptions();
            pipelineStorageOptions.Table = SqlDataTableName;
            pipelineStorageOptions.KeyColumn = SqlDataPrimaryKeyColumn;
            pipelineStorageOptions.ValueColumn = SqlDataValueColumn;

            // Create a connection factory that creates and releases a SQL connection
            pipelineStorageOptions.ConnectionFactory = new SqlConnectionFactory(SqlConnectionString);

            // A serializer to to convert job contexts into flat text for storage
            pipelineStorageOptions.Serializer = new JsonPipelineSerializer();

            // Create the pipeline storage instance
            var pipelineStorage = new SqlPipelineStorage(pipelineStorageOptions);
            return pipelineStorage;
        }

        public static void StartServer(IPipelineStorage pipelineStorage, JobStorage hangfireStorage)
        {
            // Create a new Windsor container
            Log.Info("Building the DI/IoC container");
            var container = new WindsorContainer();

            // Register the all the dependencies for the pipeline server and register tasks, for
            // Windsor it is recommenderd to name your tasks and use LifestyleScoped
            container.Register(
                // Register pipeline server dependencies
                Component.For<IPipelineStorage>().Instance(pipelineStorage),
                Component.For<IPipelineTaskFactory>().Instance(new WindsorPipelineTaskFactory(container)),
                // Register a custom pipeline server to make use of Hangfire attributes
                Component.For<IPipelineServer>().ImplementedBy<CustomPipelineServer>(),
                // Register pipeline tasks
                Component.For<GetWebpageTask>().Named("GetWebpage").LifestyleScoped(),
                Component.For<GetWebpageTextTask>().Named("GetWebpageText").LifestyleScoped(),
                Component.For<CountWordsTask>().Named("CountWords").LifestyleScoped(),
                Component.For<LogResultTask>().Named("LogResult").LifestyleScoped());

            // Resolve the pipeline server
            Log.Info("Resolving pipeline server from container");
            _pipelineServer = container.Resolve<IPipelineServer>();

            // Setup Hangfire
            var hangfireServerOptions = new BackgroundJobServerOptions();

            // Use the PipelineJobActivator with Hangfire, which will route all Hangfire executions
            // to the pipeline server
            hangfireServerOptions.Activator = new PipelineJobActivator(_pipelineServer);

            // Build the Hangfire server using SQL Server storage
            Log.Info("Building Hangfire server");
            _hangfireServer = new BackgroundJobServer(hangfireServerOptions, hangfireStorage);
        }

        public static PipelineClient GetClient(IPipelineStorage pipelineStorage, JobStorage hangfireStorage)
        {
            // Build a Hangfire job client using SQL Storage
            Log.Info("Building Hangfire client");
            var hangfireClient = new BackgroundJobClient(hangfireStorage);

            // Create a pipeline client that wraps the Hangfire job clients
            Log.Info("Building pipeline client");
            var client = new PipelineClient(pipelineStorage, hangfireClient);
            return client;
        }

        public static void CloseServer()
        {
            Log.Info("Closing server");
            _pipelineServer.Dispose();
            _hangfireServer.Dispose();
        }
    }
}
