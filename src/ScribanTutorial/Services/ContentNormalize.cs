namespace ScribanTutorial.Services;

/// <summary>
/// Single source of truth for how rendered Scriban output is compared to the
/// expected text in <c>03-expected.txt</c>. Both the in-browser runner
/// (<c>ExerciseBlock</c>) and the build-time <c>--verify</c> tool use this so
/// they can never drift.
/// </summary>
public static class ContentNormalize
{
    /// <summary>
    /// Normalises a chunk of text for output comparison:
    /// <list type="bullet">
    /// <item>CRLF (Windows / authoring tool slip) collapsed to LF.</item>
    /// <item>ALL trailing newlines trimmed (most editors add one; some files end
    /// with several; the comparison shouldn't care either way).</item>
    /// </list>
    /// Everything else — interior whitespace, tabs, trailing spaces on a line —
    /// is significant.
    /// </summary>
    public static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd('\n');
}
