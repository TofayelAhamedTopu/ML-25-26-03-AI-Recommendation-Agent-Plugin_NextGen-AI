using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace ImportMode;

public sealed class DbStore
{
    private readonly string _conn;

    public DbStore(string dbConnectionString) => _conn = dbConnectionString;

    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        EnsureDbFolder();

        await using var db = new SqliteConnection(_conn);
        await db.OpenAsync(ct);

        var cmd = db.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Documents (
  DocumentId      TEXT PRIMARY KEY,
  FileName        TEXT NOT NULL,
  FileType        TEXT NOT NULL,
  ImportedAtUtc   TEXT NOT NULL,
  Url             TEXT NOT NULL,
  Category        TEXT NULL,
  SourceDataset   TEXT NOT NULL,
  ExtractedText   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Embeddings (
  EmbeddingId     TEXT PRIMARY KEY,
  DocumentId      TEXT NOT NULL,
  ChunkIndex      INTEGER NOT NULL,
  Model           TEXT NOT NULL,
  VectorJson      TEXT NOT NULL,
  FOREIGN KEY(DocumentId) REFERENCES Documents(DocumentId)
);

CREATE INDEX IF NOT EXISTS IX_Embeddings_DocumentId ON Embeddings(DocumentId);
CREATE INDEX IF NOT EXISTS IX_Documents_SourceDataset ON Documents(SourceDataset);
";
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpsertDocumentAsync(DocumentRow d, CancellationToken ct)
    {
        await using var db = new SqliteConnection(_conn);
        await db.OpenAsync(ct);

        var cmd = db.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Documents (DocumentId, FileName, FileType, ImportedAtUtc, Url, Category, SourceDataset, ExtractedText)
VALUES ($DocumentId, $FileName, $FileType, $ImportedAtUtc, $Url, $Category, $SourceDataset, $ExtractedText)
ON CONFLICT(DocumentId) DO UPDATE SET
  FileName=excluded.FileName,
  FileType=excluded.FileType,
  ImportedAtUtc=excluded.ImportedAtUtc,
  Url=excluded.Url,
  Category=excluded.Category,
  SourceDataset=excluded.SourceDataset,
  ExtractedText=excluded.ExtractedText;
";
        cmd.Parameters.AddWithValue("$DocumentId", d.DocumentId);
        cmd.Parameters.AddWithValue("$FileName", d.FileName);
        cmd.Parameters.AddWithValue("$FileType", d.FileType);
        cmd.Parameters.AddWithValue("$ImportedAtUtc", d.ImportedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$Url", d.Url);
        cmd.Parameters.AddWithValue("$Category", (object?)d.Category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$SourceDataset", d.SourceDataset);
        cmd.Parameters.AddWithValue("$ExtractedText", d.ExtractedText);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpsertEmbeddingAsync(EmbeddingRow e, CancellationToken ct)
    {
        await using var db = new SqliteConnection(_conn);
        await db.OpenAsync(ct);

        var cmd = db.CreateCommand();
        cmd.CommandText = @"
INSERT INTO Embeddings (EmbeddingId, DocumentId, ChunkIndex, Model, VectorJson)
VALUES ($EmbeddingId, $DocumentId, $ChunkIndex, $Model, $VectorJson)
ON CONFLICT(EmbeddingId) DO UPDATE SET
  DocumentId=excluded.DocumentId,
  ChunkIndex=excluded.ChunkIndex,
  Model=excluded.Model,
  VectorJson=excluded.VectorJson;
";
        cmd.Parameters.AddWithValue("$EmbeddingId", e.EmbeddingId);
        cmd.Parameters.AddWithValue("$DocumentId", e.DocumentId);
        cmd.Parameters.AddWithValue("$ChunkIndex", e.ChunkIndex);
        cmd.Parameters.AddWithValue("$Model", e.Model);
        cmd.Parameters.AddWithValue("$VectorJson", JsonSerializer.Serialize(e.Vector));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    private void EnsureDbFolder()
    {
        // Expect "Data Source=../localdb/recommendations.db"
        var dbPath = _conn.Split('=', 2, StringSplitOptions.TrimEntries) is { Length: 2 } parts
            ? parts[1]
            : "recommendations.db";

        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }
}
