Take a US-style phone number apart with `string.slice`.

The data has `phone = "415-555-1234"`. Produce two fields on one line:

1. The area code — the first three characters: `string.slice 0 3`.
2. The line number — everything from index `8` onward. Omit the
   length argument and `string.slice` runs through to the end of the
   string.

Expected output:

    area=415 line=1234
