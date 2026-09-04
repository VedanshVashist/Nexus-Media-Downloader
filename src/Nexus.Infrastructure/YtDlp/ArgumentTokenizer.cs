namespace Nexus.Infrastructure.YtDlp;

/// <summary>
/// Splits a free-text custom-arguments string into discrete tokens, honoring
/// single and double quotes so paths with spaces survive. The tokens are added to
/// <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> individually —
/// this never builds a shell command line, so there is no shell to inject into.
/// </summary>
public static class ArgumentTokenizer
{
    public static IReadOnlyList<string> Tokenize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inSingle = false;
        var inDouble = false;

        foreach (var ch in input)
        {
            switch (ch)
            {
                case '\'' when !inDouble:
                    inSingle = !inSingle;
                    break;
                case '\"' when !inSingle:
                    inDouble = !inDouble;
                    break;
                case ' ' or '\t' when !inSingle && !inDouble:
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    break;
                default:
                    current.Append(ch);
                    break;
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}
