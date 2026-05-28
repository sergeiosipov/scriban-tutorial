Compute the SHA-256 hash of a `payload` string, and verify the hash is
exactly 64 hex characters long.

Print `hash=<64-char-hash> length=64` where the `64` on the right is
the literal `64` (you're proving the length, not formatting it).

Pipe `payload` through `string.sha256`, capture the result, then build
the output string. With `payload = "scriban"` the hash is
`77cecf561885d164c4dd298f1bf4de1a5d20478e7078aba0741cdcef4972356e`, so
the expected output is:

    hash=77cecf561885d164c4dd298f1bf4de1a5d20478e7078aba0741cdcef4972356e length=64
