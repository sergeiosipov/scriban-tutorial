The `log` string contains two IPv4 addresses that need to be redacted
before display. Use `regex.replace` with a verbatim backtick pattern to
swap every IPv4 (four dot-separated digit groups) for the literal text
`[redacted]`. Leave timestamps like `10:55` alone — they contain digits
but no dots, so the pattern won't match them.

Expected output:

```
User logged in from [redacted] at 10:55, retry from [redacted]
```
