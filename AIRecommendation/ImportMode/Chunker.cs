
using System.Text;
using System.Text.RegularExpressions;

namespace Rag
{

    /// <summary>
    /// Custom text chunker with 5% overlap and sentence-level boundaries.
    /// - Sentences are never split across chunks.
    /// - Overlap is applied in whole sentences (5% of chunkSize).
    /// - Whitespace normalization:
    ///     * Single line breaks -> space
    ///     * 2+ consecutive line breaks -> exactly one '\n'
    ///     * Other unnecessary whitespace removed
    /// </summary>
    public class Chunker
    {
        // Marker to indicate paragraph boundaries
        private const string PARA_TOKEN = "\n";

        //Percentage of overlap between chunks
        private const double _cOverlappPct = 0.05;

        /// <summary>
        /// Splits input text into chunks of approximately 'chunkSize' characters, 
        /// maintaining sentence and paragraph boundaries.
        /// </summary>
        public List<string> ChunkText(string text, int chunkSize)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // Tokenize text into sentences and paragraph markers
            var tokens = ToParagraphSentenceTokens(text);

            var chunks = new List<string>();

            // Current list of tokens in the current chunk
            var current = new List<object>();
            int currentLen = 0;
            int overlapTarget = Math.Max(0, (int)(chunkSize * _cOverlappPct));

            foreach (var tok in tokens)
            {
                int tokLen = TokenLength(tok);

                // If adding this token would exceed the chunk size, finalize current chunk
                if (currentLen > 0 && currentLen + tokLen > chunkSize)
                {
                    chunks.Add(JoinTokens(current));

                    // Prepare overlap tokens (from end of current chunk)
                    var overlap = new List<object>();
                    int acc = 0;
                    for (int i = current.Count - 1; i >= 0 && acc < overlapTarget; i--)
                    {
                        overlap.Insert(0, current[i]);
                        acc += TokenLength(current[i]);
                    }

                    current = overlap;
                    currentLen = acc;
                }

                current.Add(tok);
                currentLen += tokLen;
            }

            // Add remaining tokens as the last chunk
            if (current.Count > 0)
                chunks.Add(JoinTokens(current));

            return chunks;
        }

        /// <summary>
        /// Normalizes and tokenizes raw text into a list of sentence and paragraph tokens.
        /// Paragraphs are separated by PARA_TOKEN.
        /// </summary>
        private static List<object> ToParagraphSentenceTokens(string raw)
        {
            //-- Normalize line endings
            string text = raw.Replace("\r\n", "\n").Replace('\r', '\n');
            //-- Remove hyphenation across line breaks (e.g., "hyphen-\nated")
            text = Regex.Replace(text, @"(?<=[A-Za-zÄÖÜäöüß])-\s*\n\s*(?=[A-Za-zÄÖÜäöüß])", "");
            //-- Remove soft hyphen
            text = text.Replace("\u00AD", "");
            //-- Remove whitespace around line breaks
            text = Regex.Replace(text, @"[ \t]*\n[ \t]*", "\n");

            //-- Unique marker for paragraph breaks
            const string SENTINEL = "\u241E";
            //-- Replace multiple newlines with sentinel
            text = Regex.Replace(text, @"\n{2,}", SENTINEL);
            //-- Replace single newlines with space
            text = text.Replace("\n", " ");
            //-- Normalize spaces
            text = Regex.Replace(text, @"[ \t]{2,}", " ");
            text = text.Trim();

            var paragraphs = text.Length == 0 ? Array.Empty<string>() : text.Split(SENTINEL);
            var tokens = new List<object>();

            for (int p = 0; p < paragraphs.Length; p++)
            {
                string para = paragraphs[p].Trim();
                // If paragraph is empty, add PARA_TOKEN only if not the last paragraph
                if (para.Length == 0)
                {
                    if (p < paragraphs.Length - 1) tokens.Add(PARA_TOKEN);
                    continue;
                }

                // Split paragraph into sentences using punctuation followed by whitespace
                var sentences = Regex.Split(para, @"(?<=[\.!\?])\s+")
                                     .Where(s => !string.IsNullOrWhiteSpace(s))
                                     .Select(CleanSentence)
                                     .ToList();

                foreach (var s in sentences)
                    tokens.Add(s);
                // Add PARA_TOKEN between paragraphs
                if (p < paragraphs.Length - 1)
                    tokens.Add(PARA_TOKEN);
            }
            // Remove consecutive duplicate paragraph tokens
            CollapseDuplicateParas(tokens);

            // Remove leading and trailing paragraph tokens
            if (tokens.Count > 0 && tokens[0] is string s0 && s0 == PARA_TOKEN) tokens.RemoveAt(0);
            if (tokens.Count > 0 && tokens[^1] is string sN && sN == PARA_TOKEN) tokens.RemoveAt(tokens.Count - 1);

            return tokens;
        }

        /// <summary>
        /// Cleans a sentence by normalizing whitespace and removing spaces before punctuation.
        /// </summary>
        private static string CleanSentence(string s)
        {
            var t = Regex.Replace(s, @"[ \t]{2,}", " ").Trim();
            // Remove spaces *before* punctuation but do NOT enforce any after.
            t = Regex.Replace(t, @"\s+([,;:\.\!\?\)])", "$1");
            return t.Trim();
        }

        /// <summary>
        /// Removes consecutive duplicate paragraph tokens to avoid empty paragraphs.
        /// </summary>
        private static void CollapseDuplicateParas(List<object> tokens)
        {
            for (int i = tokens.Count - 2; i >= 0; i--)
            {
                if (tokens[i] is string a && a == PARA_TOKEN &&
                    tokens[i + 1] is string b && b == PARA_TOKEN)
                {
                    tokens.RemoveAt(i + 1);
                }
            }
        }

        /// <summary>
        /// Returns the "length" of a token for chunking purposes.
        /// </summary>
        private static int TokenLength(object tok)
        {
            if (tok is string s)
                return s == PARA_TOKEN ? 1 : s.Length;
            return 0;
        }

        /// <summary>
        /// Joins a list of tokens (sentences and paragraph markers) into a single string.
        /// Ensures correct spacing and paragraph separation.
        /// </summary>
        private static string JoinTokens(List<object> tokens)
        {
            var sb = new StringBuilder();

            foreach (var tok in tokens)
            {
                if (tok is string s)
                {
                    if (s == PARA_TOKEN)
                    {
                        // Trim trailing space before adding paragraph break
                        while (sb.Length > 0 && sb[^1] == ' ') sb.Length--;
                        if (sb.Length == 0 || sb[^1] != '\n') sb.Append('\n');
                    }
                    else
                    {
                        // Add space if not at beginning or after newline
                        if (sb.Length > 0 && sb[^1] != '\n' && sb[^1] != ' ')
                            sb.Append(' ');
                        sb.Append(s);
                    }
                }
            }

            // Final whitespace cleanup
            var result = sb.ToString();
            result = Regex.Replace(result, @"[ \t]{2,}", " ");
            result = Regex.Replace(result, @" *\n *", "\n");
            result = Regex.Replace(result, @"\n{2,}", "\n");
            return result.Trim();
        }
    }
}