Back in lesson 09 you joined a list by hand with `for.last`:

```scriban
{{- for tag in tags -}}{{ tag }}{{ if !for.last }}, {{ end }}{{- end -}}
```

That pattern earns its keep when each element needs extra markup — but
here the elements are printed as-is with a fixed separator, so the best
practice is the standard-library one-liner. Replace the whole loop with
a single `array.join` call that joins `tags` with `', '`.
