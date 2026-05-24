# Authoring lessons

The course content lives under `src/ScribanTutorial/wwwroot/lessons/`. You
don't need to know C# to add or edit a lesson — only how the file layout works
and a few Scriban basics. This guide is the field manual.

## File layout

```
wwwroot/
  manifest.json
  lessons/
    01-basics/
      01-theory.md                 ← the read-aloud part of the lesson
      02-exercises/
        hello/
          01-description.md
          02-datamodel.json
          03-expected.txt
          04-template.txt
          05-solution.txt
        member-access/
          ...
```

The numeric prefixes are part of the filename so a directory listing keeps the
right order. Add new lessons or exercises by creating the matching directories
and adding a manifest entry.

## Quick start: add a new exercise

1. Pick a lesson folder under `wwwroot/lessons/`, e.g. `02-filters/`.
2. Create a new directory inside `02-exercises/`:
   `02-filters/02-exercises/strip-whitespace/`.
3. Create five files (UTF-8, LF endings, no BOM):

   | File | What goes in it |
   |---|---|
   | `01-description.md` | What the exercise asks. Markdown; can include fenced ` ``` ` blocks. |
   | `02-datamodel.json` | The JSON data Scriban will see (must parse cleanly). |
   | `03-expected.txt` | Byte-exact expected output. Trailing newlines are trimmed before comparison. |
   | `04-template.txt` | Starter template, usually with `???` placeholders for the bits the learner fills in. |
   | `05-solution.txt` | The known-good solution. Verified by `--verify` before commit. |

4. Open `wwwroot/manifest.json` and add an entry to the lesson's `exercises`
   array (order matters):

   ```json
   { "id": "strip-whitespace", "path": "lessons/02-filters/02-exercises/strip-whitespace" }
   ```

5. From the repo root, verify your solution actually produces the expected
   output:

   ```powershell
   dotnet run --project tools\ContentBuilder -- --verify src\ScribanTutorial\wwwroot\lessons\02-filters\02-exercises\strip-whitespace
   ```

   You want `--verify OK`. If it fails, the tool prints expected vs. actual —
   fix one of them until they agree.

6. `dotnet build` to regenerate the `.html` siblings, then `dotnet run --project src\ScribanTutorial` and reload the app.

## Adding a whole new lesson

1. Create the lesson directory: `wwwroot/lessons/05-functions/`.
2. Create `01-theory.md` and an empty `02-exercises/` subdirectory.
3. Add at least one exercise (see Quick start above).
4. Add the lesson to `manifest.json`:

   ```json
   {
     "id": "05-functions",
     "title": "Functions",
     "theoryPath": "lessons/05-functions/01-theory",
     "exercises": [
       { "id": "first", "path": "lessons/05-functions/02-exercises/first" }
     ]
   }
   ```

5. `dotnet build`; reload.

## Theory markdown conventions

- Headings: `# Top`, `## Section`, `### Subsection`. Use them — the sidebar TOC
  doesn't surface them yet, but the visual hierarchy still matters.
- Fenced code blocks with a language hint get syntax highlighted at build
  time. Use `scriban` for template snippets and `json` for data.
- The custom container `:::example` produces a side-by-side panel (template /
  data / output). Pattern:

  ````markdown
  :::example
  ```scriban
  {{ user.name | string.upcase }}
  ```
  ```json
  { "user": { "name": "ada" } }
  ```
  ```text
  ADA
  ```
  :::
  ````

  The middle `json` block is optional. Use the two-block form (`scriban` +
  `text`) when the data model is implied or trivial.

## File-format rules (critical)

- **Encoding:** UTF-8 with **no BOM**.
- **Line endings:** LF (`\n`) only. The `.gitattributes` enforces this on
  commit, but configure your editor to write LF so you don't fight it.
- **JSON must parse.** Quote your keys. Trailing commas are not allowed.
- **`03-expected.txt` is byte-exact.** The runner normalises CRLF→LF and trims
  one trailing newline; everything else (spaces, tabs, trailing whitespace in
  the middle of a line) is significant.
- **Templates are plain UTF-8 text** with Scriban tags. Multi-line OK.

## VS Code settings that help

```json
{
  "files.encoding": "utf8",
  "files.eol": "\n",
  "files.insertFinalNewline": false,
  "files.trimTrailingWhitespace": false
}
```

Trim-trailing-whitespace is off because `03-expected.txt` may legitimately
contain trailing spaces inside a line.

## Verifying before commit

`dotnet run --project tools\ContentBuilder -- --verify <exercise-dir>` parses
`05-solution.txt` with `02-datamodel.json` and compares the output to
`03-expected.txt`. Use it as your last check before pushing.

For the whole batch:

```powershell
Get-ChildItem src\ScribanTutorial\wwwroot\lessons -Recurse -Directory `
  -Filter "*" |
  Where-Object { Test-Path "$($_.FullName)\05-solution.txt" } |
  ForEach-Object { dotnet run --project tools\ContentBuilder --no-build -- --verify $_.FullName }
```

## Manifest field reference

| Field | Type | Required | What it does |
|---|---|---|---|
| `courseTitle` | string | yes | Top of the sidebar |
| `courseSubtitle` | string | yes | Below the title |
| `lessons[]` | array | yes | Order matters |
| `lessons[].id` | string | yes | URL slug — must match the folder name |
| `lessons[].title` | string | yes | Display name |
| `lessons[].theoryPath` | string | yes | Path without extension; runtime fetches `.html` |
| `lessons[].exercises[]` | array | yes | Order matters |
| `lessons[].exercises[].id` | string | yes | Stable identifier — **don't rename** (progress is keyed on it) |
| `lessons[].exercises[].path` | string | yes | Path to the exercise directory (no trailing slash) |

Avoid renaming exercise IDs after release — learners' saved progress is keyed
on `lessonId:exerciseId`, and a rename silently looks like a brand-new
exercise.
