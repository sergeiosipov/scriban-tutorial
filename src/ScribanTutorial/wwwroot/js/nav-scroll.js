// Deep-link scroll. Blazor resolves a "#exercise-<slug>" fragment at navigation
// time, but a lesson's exercises are fetched asynchronously and aren't in the
// DOM yet, so the browser's native fragment jump finds nothing and the page
// stays at the top. 010_lesson calls this once its content has rendered, so a
// search result link lands on the referenced exercise instead of the page top.
// Defined on window (served from 'self', CSP-clean) so it needs no module import.
window.scrollToAnchor = function (id) {
  const el = document.getElementById(id);
  if (!el) return false;
  el.scrollIntoView({ block: 'start' });
  return true;
};
