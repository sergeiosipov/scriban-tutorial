Print the user's full name as `<first> <last>`, but use **dot notation**
to read `first_name` and **bracket notation** (with the key as a string)
to read `last_name`. Both reach the same kind of field on the same
object — practising both syntaxes here makes it easier to recognise them
in real templates later, where the choice between `.` and `[…]`
depends on whether the key is fixed or computed.
