// $antlr-format alignTrailingComments true, columnLimit 150, minEmptyLines 1, maxEmptyLinesToKeep 1, reflowComments false, useTab false
// $antlr-format allowShortRulesOnASingleLine false, allowShortBlocksOnASingleLine true, alignSemicolons hanging, alignColons hanging

grammar lisp;

lisp_
    : s_expression+ EOF
    ;

s_expression
    : ATOMIC_SYMBOL
    | '(' s_expression '.' s_expression ')'
    | list
    ;

list
    : '(' s_expression+ ')'
    ;

ATOMIC_SYMBOL
    : LETTER ATOM_PART?
    ;

fragment ATOM_PART
    : (LETTER | NUMBER) ATOM_PART
    ;

fragment LETTER
    : [a-z]
    ;

fragment NUMBER
    : [1-9]
    ;

WS
    : [ \r\n\t]+ -> skip
    ;