defmodule StellaMessagingFfiTest do
  use ExUnit.Case
  doctest StellaMessagingFfi


  test "create_pull_socket returns a valid resource" do
    StellaMessagingFfi.load_nif();
    socket_resource = StellaMessagingFfi.create_pull_socket()
    assert is_reference(socket_resource)  # Check if it's a valid resource
  end

  test "nif load" do
    # StellaMessagingFfi.load_nif();
  end

end
