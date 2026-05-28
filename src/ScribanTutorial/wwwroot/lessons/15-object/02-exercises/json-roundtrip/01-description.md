Parse a JSON string into a Scriban value, read one field out of it,
modify the resulting object, and serialise it back to JSON.

Steps:

1. Parse `raw` (a JSON string) with `object.from_json`.
2. Add a member `valid = true` to the parsed object.
3. Pipe the modified object through `object.to_json` and print.

With `raw = '{"id":7,"name":"widget"}'` the expected output is
`{"id":7,"name":"widget","valid":true}`.
