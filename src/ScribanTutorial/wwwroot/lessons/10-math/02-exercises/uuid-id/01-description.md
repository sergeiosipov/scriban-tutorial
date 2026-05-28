Generate a UUID and verify two things about it on one line:

- Its character length (should be 36 for a standard UUID-v4).
- That `math.is_number` returns `false` on it (it's a string).

Print `len=<size> is_number=<bool>` where `<size>` is `id.size` and
`<bool>` is the result of piping the id through `math.is_number`.

Because UUIDs are random, the test verifies just the structure — pipe
the result through length and type checks. Expected output:

    len=36 is_number=false
