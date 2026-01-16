using System.Reflection;
using System.Runtime.CompilerServices;

namespace EmmyLuaDocxgen;

public static class StringExtensions
{
    public static string Tab(this string str, int n) => $"{new string(' ', n)}{str}";

    /// <summary>
    /// Tests if a string matches a wildcard pattern with * characters
    /// Uses high-performance ReadOnlySpan for pattern matching
    /// Null-safe: returns false if input is null
    /// </summary>
    /// <param name="s">The string to test (null-safe)</param>
    /// <param name="pattern">The wildcard pattern (e.g., "Namespace.*", "*Class", "Pre*Post")</param>
    /// <returns>True if the string matches the pattern, false otherwise</returns>
    public static bool MatchesWildcard(this string? s, string? pattern)
    {
        if (pattern is null) return s is null;
        if (s is null) return false;

        return MatchWildcard(s.AsSpan(), pattern.AsSpan());
    }

    private static bool MatchWildcard(ReadOnlySpan<char> s, ReadOnlySpan<char> pattern)
    {
        if (pattern.IsEmpty) return s.IsEmpty;

        var tokenCount = ParsePattern(pattern, out var prefixWildcard, out var suffixWildcard);

        return (tokenCount, prefixWildcard, suffixWildcard) switch
        {
            (0, _, _) => true,
            (1, false, false) => s.SequenceEqual(pattern),
            _ => MatchTokens(s, pattern, tokenCount, prefixWildcard, suffixWildcard)
        };
    }

    private static bool MatchTokens(ReadOnlySpan<char> s, ReadOnlySpan<char> pattern,
        int tokenCount, bool prefixWildcard, bool suffixWildcard)
    {
        Span<Range> stackTokens = stackalloc Range[tokenCount];
        ExtractTokens(pattern, stackTokens, tokenCount);
        return MatchTokens(s, pattern, stackTokens, tokenCount, prefixWildcard, suffixWildcard);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ParsePattern(ReadOnlySpan<char> pattern,
        out bool prefixWildcard, out bool suffixWildcard)
    {
        prefixWildcard = pattern[0] == '*';
        suffixWildcard = pattern[^1] == '*';

        var count = 0;
        var inToken = false;

        foreach (var ch in pattern)
        {
            if (ch != '*' && !inToken)
            {
                count++;
                inToken = true;
            }
            else if (ch == '*')
            {
                inToken = false;
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExtractTokens(ReadOnlySpan<char> pattern, Span<Range> tokens, int expectedCount)
    {
        var idx = 0;
        var start = 0;

        for (var i = 0; i < pattern.Length && idx < expectedCount; i++)
        {
            if (pattern[i] == '*')
            {
                if (i > start)
                {
                    tokens[idx++] = new Range(start, i);
                }
                start = i + 1;
            }
        }

        if (start < pattern.Length && idx < expectedCount)
        {
            tokens[idx++] = new Range(start, pattern.Length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool MatchTokens(ReadOnlySpan<char> s, ReadOnlySpan<char> pattern,
        ReadOnlySpan<Range> tokens, int tokenCount, bool prefixWildcard, bool suffixWildcard)
    {
        var pos = 0;
        var tokenIdx = 0;

        if (!prefixWildcard && tokenCount > 0)
        {
            var tokenSpan = pattern[tokens[tokenIdx++]];
            if (s.Length < tokenSpan.Length || !s.StartsWith(tokenSpan))
                return false;

            pos = tokenSpan.Length;
        }

        if (!suffixWildcard && tokenCount > 0)
        {
            var tokenSpan = pattern[tokens[tokenCount - 1]];
            var start = s.Length - tokenSpan.Length;

            if (start < pos || !s.Slice(start).SequenceEqual(tokenSpan))
                return false;
        }

        var endIdx = suffixWildcard ? tokenCount : tokenCount - 1;
        for (var i = tokenIdx; i < endIdx; i++)
        {
            var tokenSpan = pattern[tokens[i]];
            var matchPos = s.Slice(pos).IndexOf(tokenSpan);

            if (matchPos == -1)
                return false;

            pos += matchPos + tokenSpan.Length;
        }

        return suffixWildcard || pos == s.Length;
    }
}