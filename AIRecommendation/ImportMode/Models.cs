namespace ImportMode;

public sealed record DocumentRow(
    string DocumentId,
    string FileName,
    string FileType,
    DateTimeOffset ImportedAtUtc,
    string Url,
    string? Category,
    string SourceDataset,
    string ExtractedText
);

public sealed record EmbeddingRow(
    string EmbeddingId,
    string DocumentId,
    int ChunkIndex,
    string Model,
    float[] Vector
);
