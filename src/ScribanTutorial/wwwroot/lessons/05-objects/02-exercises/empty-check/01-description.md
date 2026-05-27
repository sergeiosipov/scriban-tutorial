The data model carries two objects: `cart` is `{}` (a customer with no
items in their basket) and `wishlist` is `{ "book": 1, "pen": 2 }`.

Print two lines:

1. `is cart empty? true` — using the special `empty` sentinel from
   [lesson 4](/scriban-tutorial/lesson/04-variables) and the `==`
   comparison (`cart == empty`).
2. `is wishlist non-empty? true` — using the same sentinel with the
   `!` inversion: `!(wishlist == empty)`.

Both questions are saying the same thing as `.empty?` and `!.empty?`,
just spelled with the comparison operator. Knowing both forms helps when
you read templates that interop with Liquid-flavoured snippets.
