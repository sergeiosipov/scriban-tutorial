# Basics

Scriban is a small, fast templating language for .NET. A Scriban template is just
text with two kinds of holes punched in it:

- **Expression tags** `{{ ... }}` evaluate something and print the result.
- **Statement tags** `{% ... %}` run a control-flow block but print nothing of
  their own.

Anything outside the tags is copied to the output literally.

## Variables

Inside a template, you reference values from the **data model** by name. The data
model is the JSON object the host application hands to the template engine. If
the JSON looks like this:

```json
{ "name": "Ada" }
```

then `{{ name }}` renders as `Ada`.

If the value is missing the template renders an empty string — Scriban does not
fail on null. That's gentle on the eyes but can hide bugs, so always double-check
the data model when output is unexpectedly blank.

## Member access

Nested values use the dot operator. With this data:

```json
{ "user": { "first_name": "Ada", "last_name": "Lovelace" } }
```

…you can write `{{ user.first_name }} {{ user.last_name }}` to render the full
name. Member access on a missing intermediate (e.g. `user.middle_name`) also
yields the empty string — no exception, just nothing.

## Identifiers

Identifiers are case-sensitive. Stick to snake_case in your JSON keys and reach
for them the same way in your templates. That keeps lesson examples portable
across hosts that may rename .NET property names.

Open the **Hello** exercise below to try it.
