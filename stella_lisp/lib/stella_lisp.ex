defmodule StellaLisp do

  def repl do
    read() |> eval() |> print()

    repl()
  end

  def eval(expression) when is_list(expression) do

  end

  def eval(expression) when is_binary(expression) do
    expression
  end

  def eval(expression) when is_number(expression) do
    expression
  end

  def eval(expression) when is_atom(expression) do
    expression
  end

  def read() do
    IO.gets("")
  end

  def print(input) do
    IO.puts(input)
  end


end
