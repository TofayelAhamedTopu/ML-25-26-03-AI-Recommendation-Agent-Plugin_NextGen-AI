using CsvHelper;
using System.Formats.Asn1;
using System.Globalization;

namespace ImportMode;

public sealed class SupportCsvImporter
{
    public IEnumerable<(string DocumentId, string Text, string Url, string FileName)> Read(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        foreach (var r in csv.GetRecords<SupportCsvRow>())
        {
            var text =
$"""
Category: {r.Category}
Platform: {r.Platform}
Title: {r.Title}
Tags: {r.Tags}

Problem:
{r.Problem}

Symptoms:
{r.Symptoms}

Causes:
{r.Causes}

Resolution Steps:
{r.ResolutionSteps}
""".Trim();

            var id = string.IsNullOrWhiteSpace(r.QuestionId) ? Guid.NewGuid().ToString("N") : r.QuestionId.Trim();
            var docId = $"support-{id}";
            var url = string.IsNullOrWhiteSpace(r.URL) ? "about:blank" : r.URL.Trim();
            var fileName = $"support-{id}.csv";

            yield return (docId, text, url, fileName);
        }
    }
}
