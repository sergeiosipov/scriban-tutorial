using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

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
    // Hard upper bound on Render output, enforced WHILE rendering via
    // GuardedOutput — not after. LoopLimit=100_000 stops a runaway counter
    // but doesn't stop a template that emits 50 KB per iteration; checking
    // only after Render() returns would let such a template build hundreds
    // of MB in memory before the cap ever looked. 250 KB is far past
    // anything a lesson exercise needs while keeping the worst case bounded.
    private const int OutputCapBytes = 256_000;

    // Wall-clock budget for a single render, checked from OnStepLoop and from
    // every output write. WASM is single-threaded, so a timer-based
    // cancellation (CancellationTokenSource.CancelAfter) can never fire while
    // the synchronous Render() blocks the only thread — the deadline must be
    // polled from inside the render itself.
    private const int RenderBudgetMs = 2_000;

    private const string OutputCapMessage =
        "Output exceeded 250 KB and rendering was stopped — check for a loop that prints far more than the expected output.";
    private const string TimeBudgetMessage =
        "Rendering took longer than 2 seconds and was stopped — check for a runaway or deeply nested loop.";

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

        var watch = Stopwatch.StartNew();
        var sb = new StringBuilder();
        var ctx = new GuardedContext(watch)
        {
            MemberRenamer = m => m.Name,
            LoopLimit = 100_000,
            RecursiveLimit = 100,
        };
        ctx.PushGlobal(script);
        ctx.PushOutput(new GuardedOutput(sb, watch));

        try
        {
            parsed.Render(ctx);
        }
        catch (Exception ex)
        {
            // A guard tripping inside evaluation may surface wrapped in a
            // ScriptRuntimeException — prefer the guard's friendly message.
            return new Result(false, "", FindGuardMessage(ex) ?? ex.Message);
        }

        return new Result(true, sb.ToString(), null);
    }

    private static string? FindGuardMessage(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (e is GuardException g) return g.Message;
        }
        return null;
    }

    // Distinguishes our deliberate aborts from genuine template errors.
    private sealed class GuardException : Exception
    {
        public GuardException(string message) : base(message) { }
    }

    // Polls the time budget once per loop iteration — the only place a
    // template can burn unbounded time without producing output.
    private sealed class GuardedContext : TemplateContext
    {
        private readonly Stopwatch _watch;
        public GuardedContext(Stopwatch watch) => _watch = watch;

        protected override bool OnStepLoop(ScriptLoopStatementBase loop)
        {
            if (_watch.ElapsedMilliseconds > RenderBudgetMs)
                throw new GuardException(TimeBudgetMessage);
            return base.OnStepLoop(loop);
        }
    }

    // Enforces the output cap (and re-checks the time budget) on every write,
    // so output-heavy templates are stopped in-flight instead of after the
    // damage is done.
    private sealed class GuardedOutput : IScriptOutput
    {
        private readonly StringBuilder _sb;
        private readonly Stopwatch _watch;

        public GuardedOutput(StringBuilder sb, Stopwatch watch)
        {
            _sb = sb;
            _watch = watch;
        }

        public void Write(string text, int offset, int count)
        {
            if (_sb.Length + count > OutputCapBytes)
                throw new GuardException(OutputCapMessage);
            if (_watch.ElapsedMilliseconds > RenderBudgetMs)
                throw new GuardException(TimeBudgetMessage);
            _sb.Append(text, offset, count);
        }

        public ValueTask WriteAsync(string text, int offset, int count, CancellationToken cancellationToken)
        {
            Write(text, offset, count);
            return ValueTask.CompletedTask;
        }
    }
}
