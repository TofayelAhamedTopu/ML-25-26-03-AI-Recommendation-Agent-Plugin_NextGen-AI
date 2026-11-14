namespace AIRecommendation;

/// <summary>
/// Minimal agent: maps prompt -> tool + intent.
/// </summary>
public sealed class AgentRouter
{
    public (string ToolName, string IntentText) Route(string prompt)
        => ("RecommendDocuments", (prompt ?? string.Empty).Trim());
}
