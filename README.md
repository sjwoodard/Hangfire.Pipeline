INTRO
===
Hangfire.Pipeline is a utility for processing complex jobs on the Hangfire task scheduler. Jobs consist of multiple tasks that can be run in parallel or be blocking. If tasks are set to run in parallel the data will be added to the job concurrently. If a task is not run parallel (i.e. blocking) that task will have access to all output from previously executed tasks in the job. Jobs are executed on a single Hangfire worker (i.e. single thread), but each individual task is run asynchronously. Additionally, jobs are reentrant-friendly meaning that tasks that were successfully completed won't be executed again if the job restarts.

USE CASE - SENTIMENT ANALYSIS
===
Let's say we want to build a text processor that reads documents two different services, removes stopwords, splits the documents into sentence and then runs each sentence through a sentiment analysis algorithm. We need to define five tasks:

- Task 1 and 2 will read the documents from our services and add it to our job. These tasks can be run in parallel since they do not need to access each others data.
- Task 3 needs to read the documents from tasks 1 and 2, so it cannot be run in parallel. Task 3 then applies a stopword filter and updates the results in the job.
- Task 4 needs data from task 3 so it also cannot be parallel. Task 4 then uses a RegEx pattern to split each document into sentences and updates the results in the job. 
- Finally, Task 5 runs sentiment analysis on the sentences. Again, this task cannot be parallel because it depends on data from task 4. 

SETUP
===
Hangfire.Pipeline by default uses SQL Server to store job and task state information and output. It requires a key column, which defaults to a GUID and uses the SQL Server data type uniqueidentifier and a value column which should be a varbinary(max). By default, the value column will be a GZIPed JSON string. You can use different storage options by overriding the `SqlPipelineStorage` class or implementing your own `IPipelineStorage`. You can also override or implement a new serializer class.

The default method for instantiating new tasks is via reflection, and it requires you to give a tasks fully qualified class name. There is also a Castle Windsor extension, which allows for custom task names and scoped instances -- each job will create a single scope for all tasks.

USAGE
===
This section provides basic syntax and assumes you already have a working Hangfire server and are using SQL Server. For more more detailed usage see the `example` folder.

```
// Create pipeline storage
var pipelineStorageOptions = new SqlPipelineStorageOptions();
pipelineStorageOptions.ConnectionFactory = new SqlConnectionFactory(SqlConnectionString);
pipelineStorageOptions.Table = "MyTable";
pipelineStorageOptions.KeyColumn = "MyKeyColumn"; // Default data type is uniqueidentifier
pipelineStorageOptions.ValueColumn = "MyValueColumn"; // Default data type is varbinary(max)
var pipelineStorage = new SqlPipelineStorage(pipelineStorageOptions);

// Create pipeline server
var pipelineServer = new PipelineServer(pipelineStorage);

// Tell Hangfire to run jobs on the pipeline server
JobActivator.Current = new PipelineJobActivator(pipelineServer);

// Create a pipeline job
var jobContext = new PipelineJobContext();
jobContext.Id = Guid.NewGuid().ToString();
jobContext.AddEnvironment("config", "debug"); // Available to all tasks

// Create task instructions
var taskContext = new PipelineTaskContext();
taskContext.Task = "MyNamespace.MyTaskName"; // Must implement IPipelineTask
taskContext.Id = "MyTask";
taskContext.AddArg("input", "foo");
taskContext.RunParallel = true;

// Add task to the job
jobContext.QueueTask(taskContext);

// Create a client (assumes you already created a Hangfire BackgroundJobClient)
var client = new PipelineClient(pipelineStorage, hangfireBackgroundJobClient);

// Store job so that we only need to pass an ID to our Hangfire server and not the entire serialized job context
await client.Storage.CreateJobContextAsync(jobContext, CancellationToken.None);

// Send the job to Hangfire for execution
var enqueuedJobContext = await client.EnqueueAsync(jobContext);
```