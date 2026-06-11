The data model carries two named lists — one with items in it, one
empty. Loop over `lists` and print one line per list: `Name: Has X
item(s)` when the list actually has items, `Name: Nothing to show`
when it doesn't.

Empty arrays are truthy in Scriban, so a bare `if list.items` won't
catch the empty one — guard with `(array.size list.items) > 0`. With
the data below, `Inbox` must take the first branch and `Archive` the
second, so both sides of the guard get exercised.
