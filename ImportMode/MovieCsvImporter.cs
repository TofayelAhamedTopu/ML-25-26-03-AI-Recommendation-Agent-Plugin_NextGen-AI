using CsvHelper;
using System.Formats.Asn1;
using System.Globalization;

namespace ImportMode;

public sealed class MovieCsvImporter
{
    public IEnumerable<(string DocumentId, string Text, string Url, string FileName)> Read(string csvPath)
    {
        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        foreach (var r in csv.GetRecords<MovieCsvRow>())
        {
            var text =
$"""
Title: {r.Title}
Year: {r.Year}
Genre: {r.Genre}
Director: {r.Director}
Actors: {r.Actors}
Language: {r.Language}
Country: {r.Country}
IMDB_ID: {r.IMDB_ID}

Plot:
{r.Plot}

Keywords:
{r.Keywords}
""".Trim();

            var url = !string.IsNullOrWhiteSpace(r.URL)
                ? r.URL
                : (!string.IsNullOrWhiteSpace(r.IMDB_ID) ? $"https://www.imdb.com/title/{r.IMDB_ID}" : "about:blank");

            var docId = $"movie-{r.MovieId}";
            var fileName = $"{SafeFile(r.Title)}.csv";

            yield return (docId, text, url, fileName);
        }
    }

    private static string SafeFile(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "movie";
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }
}
