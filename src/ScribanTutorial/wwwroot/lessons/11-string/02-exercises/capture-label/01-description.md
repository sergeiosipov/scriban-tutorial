`capture` renders a block's output into a variable instead of emitting
it. That lets you build a multi-part string in one place, then pipe the
whole variable through a transformation — rather than applying the
transform to each piece separately.

Fill in the two `???` blanks:
- The first starts the capture block and stores its output in `label`.
- The second closes the block.

The already-written last line then pipes `label` through
`string.upcase`.

Expected output: `ADA LOVELACE, LONDON`
