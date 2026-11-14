namespace AIRecommendation;

public sealed class AppOptions
{
    public string DbConnectionString { get; set; } = "Data Source=../localdb/recommendations.db";
    public OpenAIOptions OpenAI { get; set; } = new();
    public int TopN { get; set; } = 5;
}

public sealed class OpenAIOptions
{
    public string ApiKeyEnvVar { get; set; } = "OPENAI_API_KEY";
    public string BaseUrl { get; set; } = "https://api.openai.com";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}
