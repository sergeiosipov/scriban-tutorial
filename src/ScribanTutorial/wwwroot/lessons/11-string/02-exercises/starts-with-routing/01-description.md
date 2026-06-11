Classify a list of filenames by prefix and suffix.

Loop over `files` and print one line per entry in the form
`<name>: <tag>`, where the tag is decided in order:

1. `TEST` when the name starts with `test_` (`string.starts_with`).
2. `DOC` when the name ends with `.md` (`string.ends_with`).
3. `SRC` otherwise.

Both predicates return **bool**, so they drop straight into an
`if` / `else if` chain. Expected output:

    test_parser.py: TEST
    main.py: SRC
    notes.md: DOC
    test_api.py: TEST
