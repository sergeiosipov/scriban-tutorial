// Apply the persisted theme synchronously, before any CSS or component
// renders, so the boot shell already shows the right colour scheme.
// Loaded as a classic, synchronous <script> in <head>.
(function () {
  try {
    var t = localStorage.getItem('scriban-tutorial:theme');
    if (t !== 'light' && t !== 'dark') {
      t = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }
    document.documentElement.setAttribute('data-theme', t);
  } catch (e) { /* localStorage unavailable — stick with default light */ }
})();
