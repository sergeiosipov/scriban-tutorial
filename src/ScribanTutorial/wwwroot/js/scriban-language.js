// CodeMirror 6 stream parser for Scriban (default mode — {{ ... }} delimiters
// only; the Liquid-style {% ... %} mode lives behind a separate Scriban parser
// entry point we don't use in this course).

import { StreamLanguage } from "@codemirror/language";

const KEYWORDS = new Set([
  "if", "else", "end", "for", "in", "while", "break", "continue",
  "func", "ret", "case", "when", "with", "do", "wrap", "include", "import",
  "readonly", "capture", "tablerow", "this",
]);
const ATOMS = new Set(["true", "false", "null", "empty"]);
const BUILTIN_MODULES = new Set([
  "string", "array", "object", "math", "date", "regex", "html", "fs", "timespan", "json",
]);
const OPERATORS_RE = /^(\?\?|\?\.|==|!=|<=|>=|&&|\|\||\.\.|[<>|=+\-*/%^!?:])/;

export const scribanLanguage = StreamLanguage.define({
  name: "scriban",
  startState: () => ({ inExpr: false, inString: null }),
  token(stream, state) {
    if (!state.inExpr) {
      if (stream.match("{{-") || stream.match("{{")) {
        state.inExpr = true;
        return "brace";
      }
      stream.next();
      return null;
    }

    // Inside a {{ ... }} block.
    if (state.inString) {
      while (!stream.eol()) {
        const c = stream.next();
        if (c === "\\") { stream.next(); continue; }
        if (c === state.inString) { state.inString = null; return "string"; }
      }
      return "string";
    }

    if (stream.match(/^-?\}\}/)) { state.inExpr = false; return "brace"; }
    if (stream.match(/^#[^\r\n]*/)) return "comment";
    if (stream.peek() === '"') { stream.next(); state.inString = '"'; return "string"; }
    if (stream.peek() === "'") { stream.next(); state.inString = "'"; return "string"; }
    if (stream.match(/^`[^`]*`/)) return "string";
    if (stream.match(/^\d+(\.\d+)?([eE][+-]?\d+)?/)) return "number";
    if (stream.match(OPERATORS_RE)) return "operator";
    if (stream.match(/^[\[\](){},;]/)) return "punctuation";
    if (stream.match(/^\./)) return "punctuation";

    const word = stream.match(/^[A-Za-z_][A-Za-z_0-9]*/);
    if (word) {
      const w = word[0];
      if (KEYWORDS.has(w)) return "keyword";
      if (ATOMS.has(w)) return "atom";
      if (BUILTIN_MODULES.has(w)) return "typeName";
      return "variableName";
    }

    stream.next();
    return null;
  },
});
