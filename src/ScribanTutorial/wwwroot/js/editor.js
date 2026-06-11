// NOTE: this file is the SOURCE for wwwroot/js/editor.bundle.min.js — the
// single minified ES module the app actually loads (it does not ship itself;
// see the Content Remove entries in ScribanTutorial.csproj). After editing
// this file, scriban-language.js, json-language.js, or anything under
// wwwroot/lib/codemirror, re-run the esbuild bundling procedure documented
// in wwwroot/lib/codemirror/VERSION.txt and commit the regenerated bundle.

// We deliberately do NOT pull `basicSetup` from the "codemirror" bundle —
// basicSetup transitively imports @codemirror/search, @codemirror/autocomplete,
// and @codemirror/lint, none of which this app uses (no search bar, no
// completion popup, no lint UI). Composing extensions by hand instead saves
// ~170 KB minified across those three packages.
import {
  EditorView,
  lineNumbers,
  highlightActiveLine,
  keymap,
} from "@codemirror/view";
import { history, defaultKeymap, historyKeymap } from "@codemirror/commands";
import {
  syntaxHighlighting,
  HighlightStyle,
  indentUnit,
  foldGutter,
  bracketMatching,
} from "@codemirror/language";
import { tags as t } from "@lezer/highlight";
import { scribanLanguage } from "./scriban-language.js";
import { jsonLanguage } from "./json-language.js";

// elementId -> entry. An entry is either materialised ({ view } set) or
// pending-lazy ({ pendingText, placeholder, observer } set). Exactly one of
// `view` / `pendingText` is non-null at any time.
const editors = new Map();

// Class-based highlight style. The actual colours live in wwwroot/css/app.css
// under .hl-* rules keyed off CSS variables, so light / dark switching is a
// data-theme attribute flip — no editor re-mount required. Shared by the
// Scriban grammar and the JSON grammar; both route their token kinds into
// the same .hl-* classes.
const codeHighlight = HighlightStyle.define([
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
  { tag: t.propertyName, class: "hl-property" },
]);

// Cap and floor for the auto-fit initial height. Must match the values in
// app.css's --pane-default-cap / --editor-min-height (kept in JS too because
// computed-style lookup against a freshly-mounted element is racy).
const EDITOR_DEFAULT_CAP_PX = 300;
const EDITOR_MIN_HEIGHT_PX = 80;

// options:
//   - language: "scriban" turns on the Scriban grammar + highlights, "json"
//               the JSON grammar. Anything else (undefined, "plain") leaves
//               the editor with just the base extensions.
//   - text:     initial document.
//   - autoFit:  true (default) → JS measures content after layout and sets
//               parent.style.height to fit (clamped 80 → 300). false → the
//               parent's CSS height wins (used by the Playground, where each
//               pane is a fixed box that the editor fills via flex).
//   - lazy:     false (default) → create the EditorView immediately.
//               true → render a lightweight look-alike placeholder and only
//               materialise the real EditorView when the host first scrolls
//               into view or the placeholder is clicked / focused.
//
// There is deliberately no change callback into .NET: consumers PULL the
// current document with getValue at submit / share / persist time.
export function mount(elementId, options) {
  const parent = document.getElementById(elementId);
  if (!parent) {
    console.error("editor.js: parent element not found:", elementId);
    return;
  }
  // If anything was already mounted here (Blazor re-renders), tear it down.
  if (editors.has(elementId)) destroy(elementId);

  const opts = options || {};
  const text = opts.text ?? "";

  if (opts.lazy === true) {
    mountLazy(elementId, parent, text, opts);
    return;
  }

  const view = createView(parent, text, opts.language);
  editors.set(elementId, { view, pendingText: null, placeholder: null, observer: null });
  if (opts.autoFit !== false) queueFit(parent, view.scrollDOM);
}

function createView(parent, text, language) {
  const extensions = [
    lineNumbers(),
    highlightActiveLine(),
    history(),
    bracketMatching(),
    foldGutter(),
    keymap.of([...defaultKeymap, ...historyKeymap]),
    indentUnit.of("  "),
  ];
  if (language === "scriban") {
    extensions.push(scribanLanguage, syntaxHighlighting(codeHighlight));
  } else if (language === "json") {
    extensions.push(jsonLanguage, syntaxHighlighting(codeHighlight));
  }
  return new EditorView({ doc: text ?? "", parent, extensions });
}

// Lazy mount: a <pre> stand-in instead of a real EditorView. Borrowing the
// live editor's chrome classes (.cm-editor / .cm-scroller) lets the host
// pages' existing scoped rules (height: 100%, code font, line-height,
// overflow) style the placeholder, so the swap to the real view doesn't
// visibly jump. Materialisation triggers:
//   - the host element first intersects the viewport (observer disconnects
//     after firing), or
//   - the placeholder is clicked or focused (the editor is focused after
//     materialising so the interaction isn't lost).
function mountLazy(elementId, parent, text, opts) {
  // No IntersectionObserver (ancient browser / odd embedding) → nothing
  // would ever trigger on scroll; just mount eagerly.
  if (typeof IntersectionObserver !== "function") {
    const view = createView(parent, text, opts.language);
    editors.set(elementId, { view, pendingText: null, placeholder: null, observer: null });
    if (opts.autoFit !== false) queueFit(parent, view.scrollDOM);
    return;
  }

  const placeholder = document.createElement("pre");
  placeholder.className = "cm-lazy-placeholder cm-editor cm-scroller";
  placeholder.textContent = text;
  placeholder.tabIndex = 0; // reachable by keyboard, and lets 'focus' trigger
  placeholder.style.margin = "0";
  placeholder.style.boxSizing = "border-box";
  placeholder.style.cursor = "text";
  // Approximate the real editor's content inset: 4px vertical matches
  // .cm-content's padding; the 34px left inset is roughly where the
  // line-number gutter (1–2 digit line counts) puts the first character.
  placeholder.style.padding = "4px 2px 4px 34px";
  parent.appendChild(placeholder);

  const entry = { view: null, pendingText: text, placeholder, observer: null };
  editors.set(elementId, entry);

  const materialise = (focusAfter) => {
    // Guard against double-fire (focus then click) and against the entry
    // having been destroyed or replaced by a re-mount in the meantime.
    if (editors.get(elementId) !== entry || entry.view !== null) return;
    if (entry.observer) {
      entry.observer.disconnect();
      entry.observer = null;
    }
    entry.placeholder.remove();
    entry.placeholder = null;
    entry.view = createView(parent, entry.pendingText, opts.language);
    entry.pendingText = null;
    if (opts.autoFit !== false) queueFit(parent, entry.view.scrollDOM);
    if (focusAfter) entry.view.focus();
  };

  placeholder.addEventListener("click", () => materialise(true));
  placeholder.addEventListener("focus", () => materialise(true));

  entry.observer = new IntersectionObserver((records) => {
    if (records.some((r) => r.isIntersecting)) materialise(false);
  });
  entry.observer.observe(parent);

  // Size the host around the placeholder exactly like the real editor would
  // be sized, so nothing moves when (or before) it materialises.
  if (opts.autoFit !== false) queueFit(parent, placeholder);
}

// ---------------------------------------------------------------------------
// Batched height fit. When a lesson page mounts many editors in one render,
// per-editor "read scrollHeight, write style.height" interleaves layout reads
// and writes (a forced reflow per editor). Instead every fit request lands in
// a module-level queue flushed once per animation frame: ALL reads first,
// then ALL writes — one layout pass for the whole batch.
const fitQueue = [];
let fitScheduled = false;

function queueFit(container, contentEl) {
  fitQueue.push({ container, contentEl });
  if (fitScheduled) return;
  fitScheduled = true;
  requestAnimationFrame(flushFitQueue);
}

function flushFitQueue() {
  fitScheduled = false;
  const batch = fitQueue.splice(0, fitQueue.length);
  try {
    // Read phase: scrollHeight is the natural rendered height of the content
    // (gutters and padding included). 0 for elements detached since queueing.
    const naturals = batch.map(({ contentEl }) =>
      contentEl.isConnected ? contentEl.scrollHeight : 0);
    // Write phase.
    for (let i = 0; i < batch.length; i++) {
      const { container } = batch[i];
      if (!container.isConnected) continue;
      const target = Math.min(
        EDITOR_DEFAULT_CAP_PX,
        Math.max(EDITOR_MIN_HEIGHT_PX, naturals[i] + 2));
      container.style.height = target + "px";
    }
  } catch (e) {
    console.error("editor.js: height fit failed:", e);
  }
}

// ---------------------------------------------------------------------------

export function setValue(elementId, value) {
  const entry = editors.get(elementId);
  if (!entry) return;
  const text = value ?? "";
  if (entry.view) {
    entry.view.dispatch({
      changes: { from: 0, to: entry.view.state.doc.length, insert: text },
    });
  } else {
    entry.pendingText = text;
    if (entry.placeholder) entry.placeholder.textContent = text;
  }
}

export function getValue(elementId) {
  const entry = editors.get(elementId);
  if (!entry) return "";
  return entry.view ? entry.view.state.doc.toString() : (entry.pendingText ?? "");
}

// Safe no-op for ids that never reached mount (the .NET side records ids
// before awaiting the mount interop call, so a dispose race can hand us one).
export function destroy(elementId) {
  const entry = editors.get(elementId);
  if (!entry) return;
  if (entry.observer) entry.observer.disconnect();
  if (entry.placeholder) entry.placeholder.remove();
  if (entry.view) entry.view.destroy();
  editors.delete(elementId);
}

export function count() {
  return editors.size;
}
