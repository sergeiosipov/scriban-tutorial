Mask all phone-number digit groups in a line of text — replace every
digit with `*`, keeping non-digit punctuation. Use `string.replace`
repeatedly inside a `for` loop over `0..9` to walk each digit
character.

With `text = "call 415-555-1234 or 415-555-5678"` the expected output is
`call ***-***-**** or ***-***-****`.

The trick: each pipe through `string.replace` substitutes ALL
occurrences of one digit; chain 10 of them via the loop.
