using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace AIRecommendation;

public sealed class DbStore
{
    private readonly string _conn;

    public DbStore(string dbConnectionString) => _conn = dbConnectionString;

    public async Task<IReadOnlyList<(string DocId, string FileName, string Url, string? Category, string Dataset, float[] Vector)>> LoadAllEmbeddingsAsync(CancellationToken ct)
    {
        await using var db = new SqliteConnection(_conn);
        await db.OpenAsync(ct);

        var cmd = db.CreateCommand();
        cmd.CommandText = @"
SELECT d.DocumentId, d.FileName, d.Url, d.Category, d.SourceDataset, e.VectorJson
FROM Embeddings e
JOIN Documents d ON d.DocumentId = e.DocumentId;
";

        var list = new List<(string, string, string, string?, string, float[])>();

        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var docId = r.GetString(0);
            var file = r.GetString(1);
            var url = r.GetString(2);
            var cat = r.IsDBNull(3) ? null : r.GetString(3);
            var ds = r.GetString(4);
            var vecJson = r.GetString(5);

            var vec = JsonSerializer.Deserialize<float[]>(vecJson) ?? Array.Empty<float>();
            list.Add((docId, file, url, cat, ds, vec));
        }

        return list;
    }
}
