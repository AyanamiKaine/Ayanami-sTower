defmodule StellaMessagingFfi do
  def load_nif do
    case :erlang.load_nif("./stella_messaging", 0) do
      :ok -> IO.puts("NIF loaded successfully")
      {:error, {:load_failed, error}} -> IO.puts("NIF load failed: #{error}")
      {:error, reason} ->
        raise "NIF load failed: #{inspect(reason)}"
      end
  end

  def create_pull_socket, do: StellaMessagingFfi.create_pull_socket()

end
