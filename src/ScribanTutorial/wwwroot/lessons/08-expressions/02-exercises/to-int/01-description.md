Form posts and query strings deliver numbers as **strings**. The data
model here has `width = '12'` and `height = '7'` — both strings — so
`width + height` concatenates to `127` instead of adding.

Print the naive concatenation first, then the real sum: convert each
side with `string.to_int` before adding. Expected output:

    wrong: 127, right: 19

Heads up: your template is also checked against hidden inputs with
different digit strings, so solve it with real logic rather than
printing the expected text.
