using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System.Net;

public class GetResumeCounter
{
    private readonly ILogger _logger;

    public GetResumeCounter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GetResumeCounter>();
    }

    [Function("GetResumeCounter")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        _logger.LogInformation("Retrieving and incrementing visit count.");

        // Retrieve the connection string from environment settings
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        var table = tableClient.GetTableReference("VisitCounter");

        // Retrieve the current counter entity
        var retrieveOperation = TableOperation.Retrieve<CounterEntity>("Counter", "1");
        var result = await table.ExecuteAsync(retrieveOperation);
        var currentCounter = result.Result as CounterEntity ?? new CounterEntity("Counter", "1");

        // Increment the count
        currentCounter.Count += 1;

        // Insert or replace the entity
        var insertOrReplaceOperation = TableOperation.InsertOrReplace(currentCounter);
        await table.ExecuteAsync(insertOrReplaceOperation);

        // Create a response with the updated count
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync($"{{ \"count\": {currentCounter.Count} }}");

        return response;
    }
}

// Define the CounterEntity model to represent the Table Storage entity
public class CounterEntity : TableEntity
{
    public CounterEntity(string partitionKey, string rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }

    public CounterEntity() { }

    public int Count { get; set; }
}
