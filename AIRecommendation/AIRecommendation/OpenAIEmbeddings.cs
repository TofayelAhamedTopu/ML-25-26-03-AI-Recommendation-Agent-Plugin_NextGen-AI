using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIRecommendation;

public sealed class OpenAIEmbeddings
{
    private readonly HttpClient _http;
    private readonly OpenAIOptions _opt;

    public string ModelName => _opt.EmbeddingModel;

    public OpenAIEmbeddings(HttpClient http, OpenAIOptions opt)
    {
        _http = http;
        _opt = opt;
    }

    public async Task<float[]> CreateAsync(string text, CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable(_opt.ApiKeyEnvVar);
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException($"Missing env var: {_opt.ApiKeyEnvVar}");

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_opt.BaseUrl.TrimEnd('/')}/v1/embeddings");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        req.Content = new StringContent(
            JsonSerializer.Serialize(new { model = _opt.EmbeddingModel, input = text }),
            Encoding.UTF8,
            "application/json");

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Embeddings API error: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

        using var doc = JsonDocument.Parse(body);
        var emb = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");

        var vector = new float[emb.GetArrayLength()];
        for (int i = 0; i < vector.Length; i++) vector[i] = emb[i].GetSingle();
        return vector;
    }
}
