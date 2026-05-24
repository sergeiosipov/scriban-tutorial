using System.Text;
using System.Web;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace ContentBuilder;

/// <summary>
/// Tokenises code blocks with a TextMate grammar and emits <c>&lt;span class="hl-…"&gt;</c>
/// wrappers. The actual colours live in CSS variables in <c>wwwroot/css/app.css</c>
/// so that the same palette drives both the rendered theory HTML and the
/// CodeMirror editor, and so dark / light switching is a one-attribute flip
/// on <c>&lt;html data-theme&gt;</c> instead of a re-render of every code block.
/// </summary>
internal sealed class TextMateHighlighter
{
    private readonly Registry _registry;
    private readonly RegistryOptions _options;
    private readonly IGrammar? _scribanGrammar;

    // Order matters: longest / most-specific scope prefix first. The first
    // matching scope on a token wins. Class names line up with .hl-* rules in
    // wwwroot/css/app.css and the CodeMirror HighlightStyle in editor.js.
    private static readonly (string Prefix, string Class)[] ScopeMap =
    {
        ("comment",                       "hl-comment"),
        ("string.quoted",                 "hl-string"),
        ("string.template",               "hl-string"),
        ("string.regexp",                 "hl-regexp"),
        ("string",                        "hl-string"),
        ("constant.character.escape",     "hl-escape"),
        ("constant.numeric",              "hl-number"),
        ("constant.language",             "hl-atom"),
        ("constant",                      "hl-atom"),
        ("keyword.control",               "hl-keyword"),
        ("keyword.operator",              "hl-operator"),
        ("keyword.other",                 "hl-keyword"),
        ("keyword",                       "hl-keyword"),
        ("storage",                       "hl-keyword"),
        ("punctuation.section.embedded",  "hl-brace"),
        ("punctuation.definition.string", "hl-string"),
        ("punctuation",                   "hl-punctuation"),
        ("support.type.property-name",    "hl-property"),
        ("support.class",                 "hl-type"),
        ("support.function",              "hl-function"),
        ("support.type",                  "hl-type"),
        ("entity.name.function",          "hl-function"),
        ("entity.name.type",              "hl-type"),
        ("entity.name.tag",               "hl-tag"),
        ("variable.other.member",         "hl-property"),
        ("variable.other",                "hl-variable"),
        ("variable.parameter",            "hl-variable"),
        ("variable",                      "hl-variable"),
    };

    public TextMateHighlighter(string scribanGrammarPath, string _ignoredThemeName)
    {
        // We still create a registry because LoadGrammar uses it, but the theme
        // is unused in class-emission mode. Pin to LightPlus so scope resolution
        // is deterministic.
        _options = new RegistryOptions(ThemeName.LightPlus);
        _registry = new Registry(_options);

        if (!File.Exists(scribanGrammarPath))
            throw new FileNotFoundException("Scriban grammar not found.", scribanGrammarPath);
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
                var cls = ResolveClass(token.Scopes);
                if (cls is null)
                {
                    sb.Append(Escape(raw));
                }
                else
                {
                    sb.Append("<span class=\"").Append(cls).Append("\">").Append(Escape(raw)).Append("</span>");
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

    private static string? ResolveClass(IList<string> scopes)
    {
        // Scopes are listed from outermost (e.g. "source.scriban") to innermost
        // (e.g. "string.quoted.double.scriban"). The innermost is the most
        // specific, so iterate in reverse and pick the first map hit.
        for (var i = scopes.Count - 1; i >= 0; i--)
        {
            var scope = scopes[i];
            foreach (var (prefix, cls) in ScopeMap)
            {
                if (scope.StartsWith(prefix, StringComparison.Ordinal)) return cls;
            }
        }
        return null;
    }

    private static string Escape(string s) => HttpUtility.HtmlEncode(s);
}
