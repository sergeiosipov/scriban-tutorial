// Single Page Apps for GitHub Pages — companion to 404.html.
// Decodes the ?/<path> query and history.replaceState's it back to the
// original URL so the Blazor router sees the right path on first paint.
// Loaded as a classic, synchronous <script> in <head> BEFORE <base> so the
// URL we replaceState to is correct.
(function (l) {
  if (l.search[1] === '/') {
    var decoded = l.search.slice(1).split('&').map(function (s) {
      return s.replace(/~and~/g, '&');
    }).join('?');
    window.history.replaceState(null, null, l.pathname.slice(0, -1) + decoded + l.hash);
  }
}(window.location));
