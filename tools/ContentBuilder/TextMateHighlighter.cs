using System.Text;
using System.Web;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace ContentBuilder;

internal sealed class TextMateHighlighter
{
    private readonly Registry _registry;
    private readonly RegistryOptions _options;
    private readonly Theme _theme;
    private readonly IGrammar? _scribanGrammar;

    public TextMateHighlighter(string scribanGrammarPath, string themeName)
    {
        var resolved = themeName.Equals("dark", StringComparison.OrdinalIgnoreCase)
            ? ThemeName.DarkPlus
            : ThemeName.LightPlus;
        _options = new RegistryOptions(resolved);
        _registry = new Registry(_options);
        _theme = _registry.GetTheme();

        if (!File.Exists(scribanGrammarPath))
            throw new FileNotFoundException("Scriban grammar not found.", scribanGrammarPath);
        // The grammar JSON's own "scopeName" supplies the registered scope.
        _scribanGrammar = _registry.LoadGrammarFromPathSync(scribanGrammarPath, 0, null);
    }

    public string Highlight(string code, string language)
    {
        var grammar = ResolveGrammar(language);
        if (grammar is null) return Escape(code);

        var sb = new StringBuilder();
        IStateStack? prev = null;
        var lines = code.Replace("\r\n", "\n").Split('\n');
        for (var li = 0; li < lines.Length; li++)
        {
            var line = lines[li];
            var result = grammar.TokenizeLine(line, prev, TimeSpan.FromSeconds(2));
            prev = result.RuleStack;
            foreach (var token in result.Tokens)
            {
                var startIndex = Math.Min(token.StartIndex, line.Length);
                var endIndex = Math.Min(token.EndIndex, line.Length);
                if (endIndex <= startIndex) continue;
                var raw = line[startIndex..endIndex];
                var color = ResolveColor(token.Scopes);
                if (color is null)
                {
                    sb.Append(Escape(raw));
                }
                else
                {
                    sb.Append("<span style=\"color:").Append(color).Append("\">").Append(Escape(raw)).Append("</span>");
                }
            }
            if (li + 1 < lines.Length) sb.Append('\n');
        }
        return sb.ToString();
    }

    private IGrammar? ResolveGrammar(string language)
    {
        if (string.IsNullOrEmpty(language)) return null;
        var l = language.ToLowerInvariant();
        if (l is "scriban" or "sbn") return _scribanGrammar;
        if (l is "text" or "txt" or "plaintext") return null;
        var scope = _options.GetScopeByLanguageId(l);
        if (string.IsNullOrEmpty(scope)) return null;
        try { return _registry.LoadGrammar(scope); }
        catch { return null; }
    }

    private string? ResolveColor(IList<string> scopes)
    {
        var match = _theme.Match(scopes);
        if (match is null) return null;
        foreach (var rule in match)
        {
            var color = _theme.GetColor(rule.foreground);
            if (!string.IsNullOrEmpty(color)) return color;
        }
        return null;
    }

    private static string Escape(string s) => HttpUtility.HtmlEncode(s);
}
