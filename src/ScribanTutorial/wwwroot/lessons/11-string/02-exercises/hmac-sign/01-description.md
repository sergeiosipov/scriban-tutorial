Sign a payload with a keyed hash and verify the signature length.

`string.hmac_sha256` works like `string.sha256` but takes a `secret`
as a second argument — only holders of the secret can reproduce (and
so verify) the digest, which makes it a signature rather than a plain
checksum.

Print `sig=<64-char-signature> length=64` where the `64` on the right
is the literal `64` (you're proving the length, not formatting it).

Pipe `payload` through `string.hmac_sha256 secret`, capture the
result, then build the output string. With `payload = "scriban"` and
`secret = "s3cret"` the signature is
`4eb0f79f6cbbd81e5d4be16bf8d019325aa85d1845afbdacd0d1ce01e6f53f05`, so
the expected output is:

    sig=4eb0f79f6cbbd81e5d4be16bf8d019325aa85d1845afbdacd0d1ce01e6f53f05 length=64
