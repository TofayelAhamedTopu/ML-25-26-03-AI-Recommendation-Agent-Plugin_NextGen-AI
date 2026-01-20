namespace ImportMode;

public sealed class AppOptions
{
    public string DbConnectionString { get; set; } = "Data Source=../localdb/recommendations.db";
    public OpenAIOptions OpenAI { get; set; } = new();
    public ImportOptions Import { get; set; } = new();
    public int ChunkSize { get; set; } = 1200;
}

public sealed class OpenAIOptions
{
    public string ApiKeyEnvVar { get; set; } = "OPENAI_API_KEY";
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}

public sealed class ImportOptions
{
    public string MoviesCsvPath { get; set; } = "../data/csv/movies.csv";
    public string SupportCsvPath { get; set; } = "../data/csv/support_questions.csv";
}
