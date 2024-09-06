import pytest
import stella_lisp.parser as parser
from tatsu import parse
from tatsu.util import asjson

def test_empty_list_parsing():
    """
    Tests that () is correctly parsed as an empty list
    """

    actual_ast = parse(parser.grammar, '(test (add 2 2))')
    expected_ast = 'test'
    
    
    assert actual_ast == expected_ast

