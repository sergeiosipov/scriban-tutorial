import { EditorView, basicSetup } from "codemirror";
import {
  syntaxHighlighting,
  HighlightStyle,
  indentUnit,
} from "@codemirror/language";
import { tags as t } from "@lezer/highlight";
import { scribanLanguage } from "./scriban-language.js";

const editors = new Map();

const lightStyle = HighlightStyle.define([
  { tag: t.keyword,      color: "#af00db" },
  { tag: t.atom,         color: "#0000ff" },
  { tag: t.string,       color: "#a31515" },
  { tag: t.number,       color: "#098658" },
  { tag: t.comment,      color: "#008000", fontStyle: "italic" },
  { tag: t.operator,     color: "#000000" },
  { tag: t.brace,        color: "#a31515", fontWeight: "bold" },
  { tag: t.punctuation,  color: "#000000" },
  { tag: t.variableName, color: "#001080" },
  { tag: t.typeName,     color: "#267f99" },
]);

const darkStyle = HighlightStyle.define([
  { tag: t.keyword,      color: "#c586c0" },
  { tag: t.atom,         color: "#569cd6" },
  { tag: t.string,       color: "#ce9178" },
  { tag: t.number,       color: "#b5cea8" },
  { tag: t.comment,      color: "#6a9955", fontStyle: "italic" },
  { tag: t.operator,     color: "#d4d4d4" },
  { tag: t.brace,        color: "#dcdcaa", fontWeight: "bold" },
  { tag: t.punctuation,  color: "#d4d4d4" },
  { tag: t.variableName, color: "#9cdcfe" },
  { tag: t.typeName,     color: "#4ec9b0" },
]);

export function mount(elementId, initial, dotnetRef, isDark) {
  const parent = document.getElementById(elementId);
  if (!parent) {
    console.error("editor.js: parent element not found:", elementId);
    return;
  }
  // If a view was already mounted (Blazor re-renders), tear it down first.
  if (editors.has(elementId)) destroy(elementId);

  const view = new EditorView({
    doc: initial ?? "",
    parent,
    extensions: [
      basicSetup,
      scribanLanguage,
      syntaxHighlighting(isDark ? darkStyle : lightStyle),
      indentUnit.of("  "),
      EditorView.updateListener.of((u) => {
        if (u.docChanged && dotnetRef) {
          const text = u.state.doc.toString();
          dotnetRef.invokeMethodAsync("OnEditorChange", text).catch((err) =>
            console.error("editor.js: OnEditorChange invoke failed:", err));
        }
      }),
    ],
  });
  editors.set(elementId, view);
}

export function setValue(elementId, value) {
  const view = editors.get(elementId);
  if (!view) return;
  view.dispatch({
    changes: { from: 0, to: view.state.doc.length, insert: value ?? "" },
  });
}

export function destroy(elementId) {
  const view = editors.get(elementId);
  if (view) {
    view.destroy();
    editors.delete(elementId);
  }
}

export function count() {
  return editors.size;
}
