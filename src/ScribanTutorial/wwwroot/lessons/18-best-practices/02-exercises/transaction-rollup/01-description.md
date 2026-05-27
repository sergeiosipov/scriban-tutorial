You receive four fund transactions. Roll them up into one line per
**fund + direction** (e.g. `ABC buy`), summing the `amount`. Two rules:

- **Filter** out any transaction whose `status` is not `"settled"`
  (so a `"pending"` transaction never contributes to a total).
- **Merge** transactions with the same `fund` and `type` by adding
  their amounts together.

With the data model below the expected output is:

```
ABC buy 150
XYZ sell 200
```

(`T-001` + `T-002` merge into `ABC buy 150`; `T-003` is `XYZ sell 200`;
`T-004` is filtered out because it's pending.)

Hints. There's no built-in `group_by` you need here — build the rollup
as you go using a plain object as a map keyed by `"<fund> <type>"`.
`(totals[key] ?? 0) + t.amount` is the accumulator idiom: zero on the
first sight of a key, sum thereafter. To iterate the result in insertion
order, walk `object.keys totals` inside a final `for`.
