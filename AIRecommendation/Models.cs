namespace AIRecommendation;

public sealed record RecommendationResult(
    string DocumentId,
    string FileName,
    string SourceDataset,
    string Url,
    string? Category,
    double Similarity
);
