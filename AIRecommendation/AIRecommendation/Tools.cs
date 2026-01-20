using System.Text;

namespace AIRecommendation;

public interface ITool
{
    string Name { get; }
    Task<string> ExecuteAsync(string intentText, CancellationToken ct);
}

/// <summary>
/// Plug-in tool: intent embedding -> cosine similarity -> top-N output.
/// </summary>
public sealed class RecommendDocumentsTool : ITool
{
    private readonly OpenAIEmbeddings _emb;
    private readonly DbStore _store;
    private readonly VectorMath _math;
    private readonly int _topN;

    public string Name => "RecommendDocuments";

    public RecommendDocumentsTool(OpenAIEmbeddings emb, DbStore store, VectorMath math, int topN)
    {
        _emb = emb;
        _store = store;
        _math = math;
        _topN = Math.Max(1, topN);
    }

    public async Task<string> ExecuteAsync(string intentText, CancellationToken ct)
    {
        var q = await _emb.CreateAsync(intentText, ct);
        var all = await _store.LoadAllEmbeddingsAsync(ct);

        // Best score per document (max over chunks)
        var best = new Dictionary<string, RecommendationResult>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in all)
        {
            var (docId, file, url, cat, ds, vec) = row;
            if (vec.Length != q.Length) continue;

            var sim = _math.CosineSimilarity(q, vec);

            if (!best.TryGetValue(docId, out var existing) || sim > existing.Similarity)
                best[docId] = new RecommendationResult(docId, file, ds, url, cat, sim);
        }

        var ranked = best.Values
            .OrderByDescending(x => x.Similarity)
            .Take(_topN)
            .ToList();

        return Format(ranked);
    }

    private static string Format(IReadOnlyList<RecommendationResult> ranked)
    {
        if (ranked.Count == 0) return "No recommendations found.";

        var sb = new StringBuilder();
        var top = ranked[0];

        sb.AppendLine("Top Recommendation:");
        sb.AppendLine("--------------------------------");
        sb.AppendLine($"File:       {top.FileName}");
        sb.AppendLine($"Dataset:    {top.SourceDataset}");
        sb.AppendLine($"Category:   {top.Category ?? "-"}");
        sb.AppendLine($"Similarity: {top.Similarity:F4}");
        sb.AppendLine($"URL:        {top.Url}");
        sb.AppendLine();

        sb.AppendLine($"Top {ranked.Count} Recommendations:");
        for (int i = 0; i < ranked.Count; i++)
        {
            var r = ranked[i];
            sb.AppendLine($"{i + 1}. {r.FileName} | {r.SourceDataset} | sim={r.Similarity:F4} | {r.Url}");
        }

        return sb.ToString();
    }
}
