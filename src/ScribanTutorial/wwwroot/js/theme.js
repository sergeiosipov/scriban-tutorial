// Theme persistence + DOM attribute helpers. Mirrors the inline boot script
// in index.html (which sets <html data-theme> before any CSS loads) so the
// .NET-side ThemeService can read and update without ever calling eval.

const STORAGE_KEY = "scriban-tutorial:theme";

export function getCurrent() {
  return document.documentElement.getAttribute("data-theme") || "light";
}

export function setCurrent(theme) {
  // Caller passes a validated "light" | "dark"; we still guard here so a stray
  // value can never break out of the attribute.
  const safe = theme === "dark" ? "dark" : "light";
  document.documentElement.setAttribute("data-theme", safe);
  try { localStorage.setItem(STORAGE_KEY, safe); } catch (e) { /* private mode etc. */ }
}
