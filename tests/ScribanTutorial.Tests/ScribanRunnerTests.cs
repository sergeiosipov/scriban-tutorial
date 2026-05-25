using ScribanTutorial.Services;

namespace ScribanTutorial.Tests;

public class ScribanRunnerTests
{
    [Fact]
    public void Renders_a_simple_template()
    {
        var result = ScribanRunner.Run("Hello, {{ name }}!", """{ "name": "Ada" }""");
        Assert.True(result.Ok);
        Assert.Null(result.Errors);
        Assert.Equal("Hello, Ada!", result.Output);
    }

    [Fact]
    public void Reports_friendly_message_when_data_model_is_invalid_json()
    {
        var result = ScribanRunner.Run("x", "{ not json");
        Assert.False(result.Ok);
        Assert.NotNull(result.Errors);
        Assert.StartsWith("Data model isn't valid JSON:", result.Errors);
    }

    [Fact]
    public void Reports_parse_errors_when_template_is_malformed()
    {
        // `{{ if x }}` without a matching `{{ end }}` is a Scriban parse error.
        var result = ScribanRunner.Run("{{ if x }}", """{ "x": 1 }""");
        Assert.False(result.Ok);
        Assert.NotNull(result.Errors);
        Assert.Equal("", result.Output);
    }

    [Fact]
    public void Caps_runaway_output_at_250_KB()
    {
        // 50 KB per iteration × 6 iterations ≈ 300 KB, well past the 250 KB cap.
        var template = "{{ for i in 1..6 }}" + new string('x', 50_000) + "{{ end }}";
        var result = ScribanRunner.Run(template, "{}");
        Assert.True(result.Ok);
        Assert.True(result.Output.Length <= 256_000 + 64,
            $"output length {result.Output.Length} exceeded cap + suffix budget");
        Assert.EndsWith("[output capped at 250 KB]", result.Output);
    }
}
