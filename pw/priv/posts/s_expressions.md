%{
  published: false,
  title: "Why S-Expressions really matter",
  category: "computer-science"
}
---

Its not about the ability to make it possible to manipulate the abstract syntax tree (AST). Many languages not implemented with s-expressions have shown that you can implement macros. Nim and Rust just to name a few.

The important part is not the abstraction itself but the ease of implementation. With S-Expressions you can easily implement macros without introducing to much complexity with the ability to manipulate the AST. 

Using S-Expressions as your syntax of your language makes it **incredible easy to write a parser and tokenizer**. Often this is so easy to lisp implementations bundle them into a so called **reader**, a reader is a function that parses a S-Expression into an tree-like representation that can be traversed. The canonical representation is a linked-list, but nothing really stops you using an array to represent the same structure. A linked-list is simply better as its less complex to insert, read, delete, evaluate elements from a linked list in terms of using macros at compile time.

## S-Expressions make procedures on data unambiguous

(+ 2 2 (`*`100 20))

Always means first calculating 100 times 20 and then calculating 2 + 2 + 2000, there is not need to write an precedence algorithm that handles 100 + 200 `*` 123 correctly

The correct order to resolve an expression is directly expressed in the S-Expression itself.

In order words there is not need to handle such logic explicitly this gets us reduced complexity without a reduction in expressivnes.