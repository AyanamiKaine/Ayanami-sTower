defmodule Dictionary do

  alias Dictionary.Impl.WordList
  @opaque t :: WordList.t


  defdelegate start(), to: WordList, as: :word_list
  defdelegate random_word(word_list), to: WordList
end
