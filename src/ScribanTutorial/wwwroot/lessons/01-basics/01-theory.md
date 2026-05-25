A Scriban template is just text with `{{ ... }}` tags punched in it. The same
`{{ ... }}` form is used for both **expressions** (which print their result)
and **control flow** (`{{ if cond }}…{{ else }}…{{ end }}`,
`{{ for x in items }}…{{ end }}`, and so on). Anything outside the tags is
copied to the output literally.

## Variables

Inside a tag, you reference values from the **data model** by name. The data
model is the JSON object the host hands to the engine. If the JSON is:

```json
{ "name": "Ada" }
```

then `{{ name }}` renders as `Ada`.

If the value is missing the template renders an empty string — Scriban does not
fail on null. That's gentle on the eyes but can hide bugs, so always double-check
the data model when output is unexpectedly blank.

## Member access

Nested values use the dot operator. Try it side-by-side:

:::example
```scriban
{{ user.first_name }} {{ user.last_name }}
```
```json
{ "user": { "first_name": "Ada", "last_name": "Lovelace" } }
```
```text
Ada Lovelace
```
:::

Member access on a missing intermediate (e.g. `user.middle_name`) also yields
the empty string — no exception, just nothing.

## Whitespace control

A bare `{{ tag }}` keeps the whitespace around it. Add a dash on either side to
trim it: `{{- expr -}}` removes whitespace and one surrounding newline on the
indicated side. We'll lean on this when we get to loops in lesson 03.

## Identifiers and casing

Identifiers are case-sensitive. Stick to `snake_case` in your JSON keys and
reach for them the same way in your templates — that's the convention Scriban
uses by default and the safest pattern across hosts that may rename .NET
property names.

Open the **Hello** and **Member access** exercises below to try it.
