defmodule Dictionary do

  # This is a module attribute, its created at compile time
  @word_list "./assets/words.txt"
    |> File.read!
    |> String.split("\n", trim: true)

  def random_word do
    @word_list
    |> Enum.random()
  end
end
