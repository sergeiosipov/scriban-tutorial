The earlier lessons introduced expressions, filters, and control flow one at a
time. In real templates you'll usually stack all three in a single file. This
lesson does exactly that — you'll build a small invoice template that:

- pulls the customer name from the data model,
- iterates over a list of line items,
- uses a per-line subtotal from the data,
- prints a grand total at the bottom.

## Whitespace, one more time

Whenever you mix a literal text frame with a `for` loop, the dashes on `{{-`
and `-}}` decide whether the output looks like a clean document or a list of
lines with random blank gaps. The exercise template uses dashes liberally.

:::example
```scriban
Header
{{- for n in 1..3 }}
  - line {{ n }}
{{- end }}
Footer
```
```text
Header
  - line 1
  - line 2
  - line 3
Footer
```
:::

The leading `{{-` strips the newline from the previous line. The trailing
`{{-` on `end` strips the newline immediately before it.

## Pre-computed values

Scriban can do arithmetic on the fly, but doing sums over a list inside a
template is awkward. The cleaner pattern is to pre-compute totals in the data
model (the host code that produces the JSON) and just print them. The
exercise's data follows that pattern: each line carries its own `subtotal`,
and the document carries a `total`.

That's enough — go build the invoice.
