Build a search URL by URL-encoding a query string. The data has
`base = "https://example.com/search"` and `q = "ada lovelace & co"`.

Construct `<base>?q=<encoded>` where `<encoded>` is `q` after
`html.url_encode`.

Expected output: `https://example.com/search?q=ada%20lovelace%20%26%20co`.
