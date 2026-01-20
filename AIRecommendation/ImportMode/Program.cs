using ImportMode;
using Microsoft.Extensions.Configuration;
using Rag;

Console.WriteLine("=== Import Mode (CSV -> Chunker -> Embeddings -> Local DB) ===");

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var opt = config.Get<AppOptions>() ?? new AppOptions();

var store = new DbStore(opt.DbConnectionString);
await store.EnsureSchemaAsync(CancellationToken.None);

using var http = new HttpClient();
var embeddings = new OpenAIEmbeddings(http, opt.OpenAI);

// professor chunker (unchanged)
var chunker = new Chunker();

// importers
var movieImporter = new MovieCsvImporter();
var supportImporter = new SupportCsvImporter();

var moviesCsv = FullPath(opt.Import.MoviesCsvPath);
var supportCsv = FullPath(opt.Import.SupportCsvPath);

Console.WriteLine($"Movies CSV : {moviesCsv}");
Console.WriteLine($"Support CSV: {supportCsv}");
Console.WriteLine();

if (!File.Exists(moviesCsv))
{
    Console.WriteLine("[ERROR] movies.csv not found. Check Import:MoviesCsvPath");
    return;
}

if (!File.Exists(supportCsv))
{
    Console.WriteLine("[ERROR] support_questions.csv not found. Check Import:SupportCsvPath");
    return;
}

// ---------- MOVIES ----------
Console.WriteLine("[1/2] Importing Movies CSV...");
await ImportCsvDatasetAsync(
    datasetName: "Movies",
    category: "Movie",
    records: movieImporter.Read(moviesCsv),
    store: store,
    chunker: chunker,
    embeddings: embeddings,
    chunkSize: opt.ChunkSize);

Console.WriteLine();

// ---------- SUPPORT ----------
Console.WriteLine("[2/2] Importing Support CSV...");
await ImportCsvDatasetAsync(
    datasetName: "UserSupport",
    category: "Support",
    records: supportImporter.Read(supportCsv),
    store: store,
    chunker: chunker,
    embeddings: embeddings,
    chunkSize: opt.ChunkSize);

Console.WriteLine("\n[DONE] Import finished.");

static async Task ImportCsvDatasetAsync(
    string datasetName,
    string category,
    IEnumerable<(string DocumentId, string Text, string Url, string FileName)> records,
    DbStore store,
    Chunker chunker,
    OpenAIEmbeddings embeddings,
    int chunkSize)
{
    int docCount = 0;

    foreach (var r in records)
    {
        docCount++;

        var docRow = new DocumentRow(
            DocumentId: r.DocumentId,
            FileName: r.FileName,
            FileType: "csv",
            ImportedAtUtc: DateTimeOffset.UtcNow,
            Url: r.Url,
            Category: category,
            SourceDataset: datasetName,
            ExtractedText: r.Text
        );

        await store.UpsertDocumentAsync(docRow, CancellationToken.None);

        var chunks = chunker.ChunkText(r.Text, chunkSize);
        for (int i = 0; i < chunks.Count; i++)
        {
            var vec = await embeddings.CreateAsync(chunks[i], CancellationToken.None);

            var embRow = new EmbeddingRow(
                EmbeddingId: $"{r.DocumentId}:{i}",
                DocumentId: r.DocumentId,
                ChunkIndex: i,
                Model: embeddings.ModelName,
                Vector: vec
            );

            await store.UpsertEmbeddingAsync(embRow, CancellationToken.None);
        }

        if (docCount % 100 == 0)
            Console.WriteLine($"  imported {docCount} documents...");
    }

    Console.WriteLine($"  imported total: {docCount} documents.");
}

static string FullPath(string relative) =>
    Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relative));
