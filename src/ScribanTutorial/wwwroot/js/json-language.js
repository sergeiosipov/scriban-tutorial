// Tiny JSON StreamLanguage for the Playground's data-model editor. The full
// @codemirror/lang-json is overkill for what this needs — a single-pass
// tokeniser that distinguishes object keys from value strings, plus numbers,
// atoms (true/false/null), and structural punctuation.

import { StreamLanguage } from "@codemirror/language";

export const jsonLanguage = StreamLanguage.define({
  name: "json",
  startState: () => ({ inString: false }),
  token(stream, state) {
    if (state.inString) {
      while (!stream.eol()) {
        const c = stream.next();
        if (c === "\\") { stream.next(); continue; }
        if (c === '"') {
          state.inString = false;
          // Peek for `<whitespace>:` to spot a key vs a value string. Cheap
          // and good enough — JSON's grammar makes this unambiguous at the
          // end-of-string position.
          const tail = stream.string.slice(stream.pos);
          return /^\s*:/.test(tail) ? "propertyName" : "string";
        }
      }
      return "string";
    }

    if (stream.eatSpace()) return null;

    if (stream.peek() === '"') {
      stream.next();
      state.inString = true;
      // Return a token now so the opening quote itself gets the string colour
      // — otherwise it would render uncoloured until we finished reading the
      // closing quote.
      return "string";
    }
    if (stream.match(/^-?\d+(\.\d+)?([eE][+-]?\d+)?/)) return "number";
    if (stream.match(/^(true|false|null)\b/)) return "atom";
    if (stream.match(/^[{}\[\],:]/)) return "punctuation";

    stream.next();
    return null;
  },
});
