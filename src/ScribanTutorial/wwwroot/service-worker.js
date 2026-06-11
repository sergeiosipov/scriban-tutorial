// Hand-rolled service worker (no ServiceWorkerAssetsManifest — Blazor's
// generated PWA worker precaches from a manifest we don't want to maintain;
// runtime caching covers everything we serve). Registered by js/boot.js at
// './service-worker.js' so the GitHub Pages base path works unchanged.
//
// Why: GitHub Pages serves everything with Cache-Control: max-age=600, so any
// revisit more than ten minutes later re-validates the ~50 boot requests.
// Three tiers fix that:
//
//   1. cache-first      — fingerprinted URLs (the _framework content-hash
//                         pattern and the .br variants js/boot.js fetches;
//                         the regex would also catch a fingerprinted
//                         *.styles.css if the SDK ever fingerprints CSS
//                         referenced from index.html). The hash is derived
//                         from the content, so these are effectively
//                         immutable: a new deploy produces a new URL.
//   2. stale-while-revalidate — the content tier: manifest.json (the COURSE
//                         manifest), search-index.json, reference.json,
//                         ScribanTutorial.styles.css (stable-named), lessons/**
//                         (theory .html, bundle.json, toc.json), css/js/lib
//                         assets, and any stable-named _framework files.
//                         Served from cache instantly, refreshed in the
//                         background; at most one deploy behind.
//   3. network-first    — navigations only (request.mode === 'navigate'),
//                         with the cached app shell as offline fallback. The
//                         response is passed through untouched so the GitHub
//                         Pages 404.html -> ?/path redirect flow is never
//                         intercepted. After a first visit, the SPA shell
//                         (and whatever the visit pulled into tiers 1-2)
//                         works offline.
//
// Everything else (non-GET, cross-origin, favicon and friends) is not
// handled and goes straight to the network.
//
// CACHE BUSTING: bump CACHE_VERSION. That byte-changes this file, the browser
// installs the new worker, skipWaiting + clients.claim activate it, and the
// activate handler deletes every cache with our prefix but a different name.
// The prefix guard matters: GitHub Pages project sites share the
// *.github.io origin, so cache storage is shared with any other project site.

const CACHE_VERSION = 'v1';
const CACHE_PREFIX = 'scriban-tutorial-';
const CACHE_NAME = CACHE_PREFIX + CACHE_VERSION;

// './' resolved against the worker URL = the app root (scope root).
const SHELL_URL = new URL('./', self.location).href;
const SCOPE_PATH = new URL('./', self.location).pathname;

// A 10-char lowercase base36 segment right before the extension (optionally
// followed by .br), e.g. dotnet.native.p7q2c8v4xz.wasm[.br], or before
// .styles.css for the scoped-CSS bundle. Exactly 10 on purpose:
// blazor.webassembly.js ("webassembly" = 11) must NOT look fingerprinted.
const FINGERPRINTED = /\.[a-z0-9]{10}\.(?:styles\.css|[a-z0-9]+)(?:\.br)?$/;

// Content-tier prefixes/names, relative to the scope root.
const CONTENT_PREFIXES = ['lessons/', 'css/', 'js/', 'lib/', '_framework/'];
const CONTENT_FILES = ['manifest.json', 'search-index.json', 'reference.json', 'ScribanTutorial.styles.css'];

self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const cache = await caches.open(CACHE_NAME);
        // Minimal precache: just the shell. Everything else fills lazily.
        await cache.add(SHELL_URL);
        await self.skipWaiting();
    })());
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        const names = await caches.keys();
        await Promise.all(names
            .filter(n => n.startsWith(CACHE_PREFIX) && n !== CACHE_NAME)
            .map(n => caches.delete(n)));
        await self.clients.claim();
    })());
});

async function cacheFirst(request) {
    const cache = await caches.open(CACHE_NAME);
    const hit = await cache.match(request);
    if (hit) {
        return hit;
    }
    const response = await fetch(request);
    if (response.ok) {
        await cache.put(request, response.clone());
    }
    return response;
}

async function staleWhileRevalidate(event, request) {
    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);
    const refresh = fetch(request).then(async response => {
        if (response.ok) {
            await cache.put(request, response.clone());
        }
        return response;
    });
    if (cached) {
        // Serve stale now, let the refresh land for next time.
        event.waitUntil(refresh.catch(() => { }));
        return cached;
    }
    return refresh;
}

async function networkFirstNavigation(request, url) {
    const cache = await caches.open(CACHE_NAME);
    try {
        const response = await fetch(request);
        // Refresh the shell copy only from the canonical root URL (the GitHub
        // Pages ?/path bounce lands there too); deep links return 404.html
        // bodies that must never become the shell.
        if (response.ok && url.pathname === SCOPE_PATH) {
            await cache.put(SHELL_URL, response.clone());
        }
        return response;
    } catch (error) {
        // Offline: serve the shell for any navigation. The Blazor router sees
        // the original deep-link path, so no ?/ bounce is needed.
        const shell = await cache.match(SHELL_URL);
        if (shell) {
            return shell;
        }
        throw error;
    }
}

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET') {
        return;
    }
    const url = new URL(request.url);
    if (url.origin !== self.location.origin) {
        return;
    }

    if (request.mode === 'navigate') {
        event.respondWith(networkFirstNavigation(request, url));
        return;
    }

    if (FINGERPRINTED.test(url.pathname)) {
        event.respondWith(cacheFirst(request));
        return;
    }

    const relative = url.pathname.startsWith(SCOPE_PATH)
        ? url.pathname.slice(SCOPE_PATH.length)
        : null;
    if (relative !== null &&
        (CONTENT_FILES.includes(relative) ||
            CONTENT_PREFIXES.some(prefix => relative.startsWith(prefix)))) {
        event.respondWith(staleWhileRevalidate(event, request));
    }
    // Anything else: fall through to the network untouched.
});
