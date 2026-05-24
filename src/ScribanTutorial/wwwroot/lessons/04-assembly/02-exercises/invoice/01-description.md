### Invoice

Render a small invoice. Expected output:

```text
Invoice for Ada Lovelace
------------------------
widget (x2): $20
gadget (x1): $25
------------------------
Total: $45
```

Use a `for` loop to iterate over `lines`, pull `description`, `qty`, and
`subtotal` from each line, then close with the grand `total`.
