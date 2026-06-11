A config object can carry a member whose value is `null` — the key is
present but holds nothing. `object.has_key` and `object.has_value`
disagree about such members: `has_key` asks "does this key exist?"
(`true` even when the value is null), while `has_value` asks "does this
key hold a non-null value?" (`false` for a null member).

The data has a `config` with a present `host` and a null `proxy`. Print
the member count via `object.size`, then both checks for each of the
two keys.

With the data below the expected output is:

    size=2
    host: has_key=true has_value=true
    proxy: has_key=true has_value=false

Note that `object.size` counts the null member too — `proxy` is a real
member of the object; it just holds `null`.
