A search-and-highlight pipeline that takes a literal user query and
wraps every occurrence of it in `[…]` brackets in the source text.

The trick: the user query might contain regex metacharacters (`(`,
`.`, `*`, etc.) that you DON'T want interpreted. Escape it first with
`regex.escape`, then use that escaped form as the pattern in
`regex.replace` with `[$0]` as the replacement (where `$0` is the
whole match).

With `text = "see (price)* and (price)*"` and `query = "(price)*"`
the expected output is `see [(price)*] and [(price)*]`.
