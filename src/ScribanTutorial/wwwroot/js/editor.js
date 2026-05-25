import { EditorView, basicSetup } from "codemirror";
import {
  syntaxHighlighting,
  HighlightStyle,
  indentUnit,
} from "@codemirror/language";
import { tags as t } from "@lezer/highlight";
import { scribanLanguage } from "./scriban-language.js";

const editors = new Map();

// Class-based highlight style. The actual colours live in wwwroot/css/app.css
// under .hl-* rules keyed off CSS variables, so light / dark switching is a
// data-theme attribute flip — no editor re-mount required.
const scribanHighlight = HighlightStyle.define([
  { tag: t.keyword,      class: "hl-keyword" },
  { tag: t.atom,         class: "hl-atom" },
  { tag: t.string,       class: "hl-string" },
  { tag: t.number,       class: "hl-number" },
  { tag: t.comment,      class: "hl-comment" },
  { tag: t.operator,     class: "hl-operator" },
  { tag: t.brace,        class: "hl-brace" },
  { tag: t.punctuation,  class: "hl-punctuation" },
  { tag: t.variableName, class: "hl-variable" },
  { tag: t.typeName,     class: "hl-type" },
]);

// Cap and floor for the auto-fit initial height. Must match the values in
// app.css's --pane-default-cap / --editor-min-height (kept in JS too because
// computed-style lookup against a freshly-mounted element is racy).
const EDITOR_DEFAULT_CAP_PX = 350;
const EDITOR_MIN_HEIGHT_PX = 80;

// options:
//   - language: "scriban" turns on the Scriban grammar + highlights. Anything
//                else (undefined, "plain") leaves the editor with just basicSetup —
//                used by the Playground for its JSON data-model field.
//   - callbackMethod: name of the [JSInvokable] method on dotnetRef to call when
//                     the document changes (defaults to "OnEditorChange").
export function mount(elementId, initial, dotnetRef, options) {
  const parent = document.getElementById(elementId);
  if (!parent) {
    console.error("editor.js: parent element not found:", elementId);
    return;
  }
  // If a view was already mounted (Blazor re-renders), tear it down first.
  if (editors.has(elementId)) destroy(elementId);

  const opts = options || {};
  const language = opts.language;
  const callbackMethod = opts.callbackMethod || "OnEditorChange";

  const extensions = [basicSetup, indentUnit.of("  ")];
  if (language === "scriban") {
    extensions.push(scribanLanguage, syntaxHighlighting(scribanHighlight));
  }
  extensions.push(EditorView.updateListener.of((u) => {
    if (u.docChanged && dotnetRef) {
      const text = u.state.doc.toString();
      dotnetRef.invokeMethodAsync(callbackMethod, text).catch((err) =>
        console.error(`editor.js: ${callbackMethod} invoke failed:`, err));
    }
  }));

  const view = new EditorView({ doc: initial ?? "", parent, extensions });
  editors.set(elementId, view);

  // CSS holds the container at --editor-min-height as a placeholder. Once
  // CodeMirror has laid out, measure its scrollHeight and set the container
  // to fit-content-up-to-cap. After that the user can drag the corner to
  // resize freely (CSS resize:vertical sets inline height on drag, which
  // overrides our initial set).
  requestAnimationFrame(() => {
    try { fitEditorHeight(parent, view); }
    catch (e) { console.error("editor.js: fitEditorHeight failed:", e); }
  });
}

function fitEditorHeight(parent, view) {
  const scroller = view.scrollDOM;
  // scrollHeight is the natural rendered height of the editor's content
  // (gutters included, padding included). Falls back to 0 if not laid out yet
  // — the requestAnimationFrame above usually avoids that.
  const natural = scroller.scrollHeight;
  const target = Math.min(
    EDITOR_DEFAULT_CAP_PX,
    Math.max(EDITOR_MIN_HEIGHT_PX, natural + 2));
  parent.style.height = target + "px";
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
