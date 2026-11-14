namespace ImportMode;

/// <summary>
/// Represents one row in support_questions.csv (headers must match exactly).
/// Columns:
/// QuestionId,Category,Title,Problem,Symptoms,Causes,ResolutionSteps,Tags,Platform,URL
/// </summary>
public sealed class SupportCsvRow
{
    public string QuestionId { get; set; } = "10";
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Problem { get; set; } = "";
    public string Symptoms { get; set; } = "";
    public string Causes { get; set; } = "";
    public string ResolutionSteps { get; set; } = "";
    public string Tags { get; set; } = "";
    public string Platform { get; set; } = "";
    public string URL { get; set; } = "";
}
