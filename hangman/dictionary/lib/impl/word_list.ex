defmodule Dictionary.Impl.WordList do

  @type t :: list(String)


  def word_list do
    "./assets/words.txt"
    |> File.read!
    |> String.split("\n", trim: true)
  end

  @spec random_word(t) :: String.t
  def random_word(word_list) do
    word_list
    |> Enum.random()
  end

end
