using System.Text.Json;
using Scriban;
using Scriban.Runtime;

namespace ScribanTutorial.Services;

/// <summary>
/// Single source of truth for parsing and rendering a Scriban template
/// against a JSON data model. The in-browser ExerciseBlock, the in-browser
/// Playground, and the build-time SolutionVerifier all funnel through here
/// so a change to LoopLimit / RecursiveLimit / MemberRenamer / output cap
/// applies everywhere at once.
/// </summary>
public static class ScribanRunner
{
    public sealed record Result(bool Ok, string Output, string? Errors);

    public static Result Run(string template, string dataJson)
    {
        ScriptObject script;
        try
        {
            using var doc = JsonDocument.Parse(dataJson);
            script = new ScriptObject();
            JsonToScriban.Import(doc.RootElement, script);
        }
        catch (JsonException ex)
        {
            return new Result(false, "", $"Data model isn't valid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new Result(false, "", ex.Message);
        }

        var parsed = Template.Parse(template);
        if (parsed.HasErrors)
        {
            return new Result(false, "", string.Join("\n", parsed.Messages.Select(m => m.ToString())));
        }

        var ctx = new TemplateContext
        {
            MemberRenamer = m => m.Name,
            LoopLimit = 100_000,
            RecursiveLimit = 100,
        };
        ctx.PushGlobal(script);

        string output;
        try
        {
            output = parsed.Render(ctx);
        }
        catch (Exception ex)
        {
            return new Result(false, "", ex.Message);
        }

        return new Result(true, output, null);
    }
}
