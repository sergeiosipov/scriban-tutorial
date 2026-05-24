// localStorage helpers for ProgressService. ES module, loaded via JSImport.
// Keys are kept simple strings; values are JSON-serialised on the .NET side.

export function get(key) {
  return localStorage.getItem(key);
}

export function set(key, value) {
  localStorage.setItem(key, value);
}

export function remove(key) {
  localStorage.removeItem(key);
}

export function listKeysWithPrefix(prefix) {
  const out = [];
  for (let i = 0; i < localStorage.length; i++) {
    const k = localStorage.key(i);
    if (k && k.startsWith(prefix)) out.push(k);
  }
  return out;
}

export function clearWithPrefix(prefix) {
  const toRemove = listKeysWithPrefix(prefix);
  for (const k of toRemove) localStorage.removeItem(k);
  return toRemove.length;
}
