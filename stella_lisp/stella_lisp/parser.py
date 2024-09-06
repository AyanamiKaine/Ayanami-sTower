from lark import Lark, Transformer

lisp_grammar = """
    ?start: sexpr

    ?sexpr: atom
          | list

    list : "(" sexpr* ")"

    ?atom: NUMBER           -> number
         | SYMBOL           -> symbol
         | STRING           -> string

    %import common.NUMBER
    %import common.WS
    %import common.ESCAPED_STRING -> STRING
    %ignore WS

    SYMBOL: /[a-zA-Z]+/ 
"""
class LispTransformer(Transformer):
    def number(self, token):
        if isinstance(token, list):
            print("Unexpected list:", token)  # Print the problematic list
                # ... (print more context here if needed)
            raise ValueError("Expected a number, got a list")
        else:
            return float(token)

    def symbol(self, token):
        return str(token)

    def string(self, token):
        return token[1:-1].replace('\\"', '"')  # Remove quotes and handle escaped quotes

    def list(self, items):
        return list(items)

def test():
    parser = Lark(lisp_grammar, parser='earley')

    lisp_code = "(+ 2 (* 3 4))"
    tree = parser.parse(lisp_code)
    print(tree.pretty())  # Print the parsed tree

    parsed_result = parser.parse(lisp_code)
    print(parsed_result)

if __name__ == '__main__':
    test()