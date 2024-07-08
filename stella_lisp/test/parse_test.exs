defmodule StellaLispParserTest do
  use ExUnit.Case
  doctest StellaLisp.Parser

  test "parse simply arimetic expression into a list" do
    input = "(+ 100 100)"

    list = StellaLisp.Parser.parse_expression(input)

    assert ["+", "100", "100"] == list
  end
end
