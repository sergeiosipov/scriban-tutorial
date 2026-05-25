// Tiny JSON StreamLanguage for the Playground's data-model editor. The full
// @codemirror/lang-json is overkill for what this needs — a single-pass
// tokeniser that distinguishes object keys from value strings, plus numbers,
// atoms (true/false/null), and structural punctuation.

import { StreamLanguage } from "@codemirror/language";

export const jsonLanguage = StreamLanguage.define({
  name: "json",
  startState: () => ({ inString: false }),
  token(stream, state) {
    // Continuation of an unterminated string from a previous line. JSON
    // forbids raw newlines in strings, so we only hit this while the user
    // is mid-typing.
    if (state.inString) {
      while (!stream.eol()) {
        const c = stream.next();
        if (c === "\\") { stream.next(); continue; }
        if (c === '"') {
          state.inString = false;
          return /^\s*:/.test(stream.string.slice(stream.pos))
            ? "propertyName" : "string";
        }
      }
      return "string";
    }

    if (stream.eatSpace()) return null;

    // Strings are emitted as a single token (both quotes + body) so the
    // open quote shares the same colour as the close quote. Returning the
    // opening quote separately would tag it as "string" before we knew the
    // close was followed by `:`, leaving the open quote rendered as
    // .hl-string while the rest got .hl-property — visibly mismatched.
    if (stream.peek() === '"') {
      stream.next();
      while (!stream.eol()) {
        const c = stream.next();
        if (c === "\\") { stream.next(); continue; }
        if (c === '"') {
          return /^\s*:/.test(stream.string.slice(stream.pos))
            ? "propertyName" : "string";
        }
      }
      // Unterminated on this line — the user is mid-typing. Stay in the
      // string state so the next line continues to consume until the
      // closing quote.
      state.inString = true;
      return "string";
    }
    if (stream.match(/^-?\d+(\.\d+)?([eE][+-]?\d+)?/)) return "number";
    if (stream.match(/^(true|false|null)\b/)) return "atom";
    if (stream.match(/^[{}\[\],:]/)) return "punctuation";

    stream.next();
    return null;
  },
});
