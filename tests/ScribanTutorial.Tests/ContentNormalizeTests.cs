using ScribanTutorial.Services;

namespace ScribanTutorial.Tests;

public class ContentNormalizeTests
{
    [Theory]
    [InlineData("Hello\r\nWorld", "Hello\nWorld")]
    [InlineData("Hello\nWorld\n", "Hello\nWorld")]
    [InlineData("Hello\r\nWorld\r\n", "Hello\nWorld")]
    [InlineData("Hello\n\n", "Hello")]        // all trailing newlines trimmed
    [InlineData("Hello\n\n\n\n", "Hello")]    // generously
    [InlineData("", "")]
    [InlineData("\n", "")]
    [InlineData("a", "a")]
    [InlineData("a\nb\nc\n", "a\nb\nc")]
    public void Normalize_collapses_crlf_and_trims_trailing_newlines(string input, string expected)
    {
        Assert.Equal(expected, ContentNormalize.Normalize(input));
    }

    [Fact]
    public void Normalize_preserves_interior_whitespace()
    {
        var input = "first line  \n\tsecond line\n  third  \n";
        var expected = "first line  \n\tsecond line\n  third  ";
        Assert.Equal(expected, ContentNormalize.Normalize(input));
    }
}
