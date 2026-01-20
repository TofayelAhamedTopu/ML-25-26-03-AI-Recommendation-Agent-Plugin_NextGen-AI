using Rag;
using System.Diagnostics.Metrics;
using Xunit;

public sealed class ChunkerTests
{
    [Fact]
    public void Chunker_CreatesMoreThanOneChunk_ForLongText()
    {
        var chunker = new Chunker();
        var text = string.Join(" ", Enumerable.Repeat("This is a sentence.", 500));
        var chunks = chunker.ChunkText(text, 1200);

        Assert.True(chunks.Count >= 2);
        Assert.All(chunks, c => Assert.False(string.IsNullOrWhiteSpace(c)));
    }
}
