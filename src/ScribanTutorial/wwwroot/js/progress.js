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

// --- Export / import (progress backup files) -------------------------------

// All matching entries as a { key: value } object. Values stay the raw
// JSON strings — .NET wraps them into the export payload untouched.
export function exportWithPrefix(prefix) {
  const out = {};
  for (const k of listKeysWithPrefix(prefix)) out[k] = localStorage.getItem(k);
  return out;
}

// Writes every { key: value } pair verbatim (values are already-serialised
// JSON strings) and returns how many were written. Existing keys are
// overwritten — validation happens on the .NET side before this is called.
export function importEntries(entries) {
  let count = 0;
  for (const [k, v] of Object.entries(entries)) {
    localStorage.setItem(k, v);
    count++;
  }
  return count;
}

// Triggers a browser download of `text` via a Blob and a temporary <a download>.
export function downloadJson(filename, text) {
  const blob = new Blob([text], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

// Opens a file picker and resolves with the chosen file's text, or null when
// the user cancels. Relies on the 'cancel' event on <input type=file>,
// supported by every browser recent enough to run .NET WASM.
export function pickJsonFile() {
  return new Promise((resolve) => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'application/json,.json';
    input.style.display = 'none';
    let settled = false;
    const settle = (value) => {
      if (settled) return;
      settled = true;
      input.remove();
      resolve(value);
    };
    input.addEventListener('change', () => {
      const file = input.files && input.files[0];
      if (!file) { settle(null); return; }
      file.text().then((text) => settle(text), () => settle(null));
    });
    input.addEventListener('cancel', () => settle(null));
    document.body.appendChild(input);
    input.click();
  });
}
