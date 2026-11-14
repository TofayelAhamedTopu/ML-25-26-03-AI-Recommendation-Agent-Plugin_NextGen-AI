using AIRecommendation;
using Microsoft.Extensions.Configuration;



Console.WriteLine("=== AI Recommendation (Console App #2) ===");
Console.WriteLine("Example: Looking for USB cable");
Console.WriteLine("Or: c:\\temp\\text.txt");
Console.WriteLine("Type 'exit' to quit.\n");

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var opt = config.Get<AppOptions>() ?? new AppOptions();

var router = new AgentRouter();
var store = new DbStore(opt.DbConnectionString);
var math = new VectorMath();

using var http = new HttpClient();
var emb = new OpenAIEmbeddings(http, opt.OpenAI);

// tool registry (plugin architecture)
var tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase)
{
    ["RecommendDocuments"] = new RecommendDocumentsTool(emb, store, math, opt.TopN)
};

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine() ?? "";
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    var prompt = ResolvePrompt(input);
    var (toolName, intentText) = router.Route(prompt);

    if (!tools.TryGetValue(toolName, out var tool))
    {
        Console.WriteLine($"Tool not found: {toolName}\n");
        continue;
    }

    try
    {
        var output = await tool.ExecuteAsync(intentText, CancellationToken.None);
        Console.WriteLine("\n" + output + "\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine("\nERROR:\n" + ex.Message + "\n");
    }
}

static string ResolvePrompt(string raw)
{
    var s = raw.Trim();
    if (File.Exists(s)) return File.ReadAllText(s);
    return s;
}
