The data model has an **empty** `items` array. Print `Has X item(s)`
where `X` is the size, but ONLY when the array actually has items.
Otherwise print `Nothing to show`.

Empty arrays are truthy in Scriban, so a bare `if items` won't catch
this — use `array.size items > 0`.
