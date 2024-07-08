defmodule StellaLisp.Parser do
  def parse_expression(expression) do
    String.trim(expression,"()")
      |> String.split(" ")
  end
end
