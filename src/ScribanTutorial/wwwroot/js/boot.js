// Boot module. Loaded (type="module") after _framework/blazor.webassembly.js,
// which carries autostart="false" so that we control startup. Two jobs:
//
//   1. Register the hand-rolled service worker (./service-worker.js — relative
//      so the GitHub Pages base path /scriban-tutorial/ keeps working).
//   2. Start Blazor with a loadBootResource that fetches the .br precompressed
//      siblings and decodes them client-side. GitHub Pages stores the publish
//      output's *.br files but never serves them with Content-Encoding (no
//      negotiation), so without this every boot asset transfers at gzip size
//      at best. This follows the official "host Blazor WASM on GitHub Pages"
//      pattern (dotnet/blazor-samples BlazorWebAssemblyXrefGenerator).
//
// Dev/Debug output has no .br siblings, so .br availability is probed exactly
// once: the first non-dotnetjs resource request doubles as the probe, and if
// its .br fetch is not ok (or throws), every resource — including that one —
// falls back to fetching its default URI. At most one request fails in dev.

import { BrotliDecode } from './brotli-decode.min.js';

// No service worker during local development: its stale-while-revalidate tier
// would serve js/css/lessons one rebuild behind on every refresh, which makes
// content authoring miserable. The worker is exercised on the deployed site.
const isLocalDev = location.hostname === 'localhost' || location.hostname === '127.0.0.1';
if ('serviceWorker' in navigator && !isLocalDev) {
    // Fire-and-forget: registration must never block or fail the boot.
    navigator.serviceWorker.register('./service-worker.js').catch(() => { });
}

// Promise of the probe's .br response: a Response when .br is served, null
// when it is not. Settled by the first non-dotnetjs resource request; every
// later request awaits the verdict instead of issuing its own doomed fetch.
let probe = null;

async function decodeToResponse(brotliResponse, type) {
    const compressed = new Int8Array(await brotliResponse.arrayBuffer());
    const decompressed = BrotliDecode(compressed);
    const contentType =
        type === 'dotnetwasm' ? 'application/wasm' : 'application/octet-stream';
    return new Response(decompressed, { headers: { 'content-type': contentType } });
}

function loadBootResource(type, name, defaultUri, integrity) {
    // The runtime's own JS modules (dotnet.js, dotnet.native.js, dotnet.runtime.js
    // all surface as type 'dotnetjs') cannot be returned as custom Responses —
    // only a URI string or undefined (default loading) is allowed.
    if (type === 'dotnetjs') {
        // With OverrideHtmlAssetPlaceholders on, the publish output ships
        // _framework files ONLY under fingerprinted names, but the loader's
        // built-in fallback imports the stable './dotnet.js'. The build
        // substituted the real fingerprinted URL into the modulepreload link
        // in index.html — hand that to the loader. (dotnet.native.js and
        // dotnet.runtime.js arrive here with already-fingerprinted URIs from
        // the boot config embedded in dotnet.js, so they load by default.)
        if (name === 'dotnet.js') {
            const preload = document.getElementById('dotnet-js-preload');
            if (preload && preload.href) {
                return preload.href;
            }
        }
        return undefined;
    }

    // Note on integrity: the boot resource loader skips integrity validation
    // for custom Response objects (documented behavior, and what the official
    // sample relies on). The fallback paths below return a plain same-origin
    // fetch of the default URI, which carries the same bytes the integrity
    // hash was computed from.
    return (async () => {
        if (probe === null) {
            // First resource: its .br fetch doubles as the availability probe.
            // (The assignment happens synchronously on the first call, before
            // the first await, so concurrent calls can never start a second
            // probe.)
            probe = fetch(defaultUri + '.br').then(r => (r.ok ? r : null), () => null);
            const probed = await probe;
            if (probed === null) {
                return fetch(defaultUri);
            }
            try {
                return await decodeToResponse(probed, type);
            } catch {
                return fetch(defaultUri);
            }
        }

        if (await probe === null) {
            return fetch(defaultUri);
        }
        const response = await fetch(defaultUri + '.br');
        if (!response.ok) {
            // .br hosting works but this one sibling is missing — recover quietly.
            return fetch(defaultUri);
        }
        try {
            return await decodeToResponse(response, type);
        } catch {
            return fetch(defaultUri);
        }
    })();
}

Blazor.start({ loadBootResource });
