Use a `case ... when ... end` block to translate a country code from the
data model into a region:

- `"US"`, `"CA"`, `"MX"` → `"Americas"`
- `"FR"`, `"DE"`, `"IT"`, `"ES"` → `"Europe"`
- `"JP"`, `"KR"`, `"CN"` → `"Asia"`
- anything else → `"Other"`

The data carries `code = "DE"`, so the expected output is `Europe`.
Use a single `when` arm with comma-separated values per region — that's
what `case` was built for.
