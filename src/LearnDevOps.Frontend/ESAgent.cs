namespace LearnDevOps.Frontend;

using Microsoft.Extensions.Logging;

public record class Doc(int Id, string Title, string Content);

public interface IESAgent
{
    Doc? QueryDocById(int id);
    void Put(Doc doc);
}

public class ESAgent : IESAgent
{
    private const string IndexName = "doc_idx";

    private readonly ILogger<ESAgent> _logger;
    private Nest.ElasticClient _client;

    public ESAgent(ILogger<ESAgent> logger)
    {
        _logger = logger;

        var uri = Environment.GetEnvironmentVariable("ES_URI").NotNull();
        string username = Environment.GetEnvironmentVariable("ES_USERNAME").NotNull();
        string password = Environment.GetEnvironmentVariable("ES_PASSWORD").NotNull();
        var pool = new Elasticsearch.Net.SingleNodeConnectionPool(new Uri(uri));
        _logger.LogInformation($"Prepare init es agent, username {username}");
        var settings = new Nest.ConnectionSettings(pool).BasicAuthentication(username, password);
        _client = new Nest.ElasticClient(settings);
    }

    public Doc? QueryDocById(int id)
    {
        var resp = _client.Get<Doc>(id, idx => idx.Index(IndexName));
        _logger.LogInformation($"Query doc by id {id}, response = {resp}, doc = {resp.Source}");
        if (!resp.IsValid) {
            if (resp.ApiCall.HttpStatusCode == 404) {
                return null;
            }
            throw new InvalidOperationException($"query failed {resp}");
        }
        return resp.Source ?? throw new AssertionException("should never be null");
    }

    public void Put(Doc doc)
    {
        var resp = _client.Index(doc, idx => idx.Index(IndexName));
        _logger.LogInformation($"Index doc {doc}, response = {resp}");
        if (!resp.IsValid) {
            throw new InvalidOperationException($"query failed {resp}");
        }
    }
}
