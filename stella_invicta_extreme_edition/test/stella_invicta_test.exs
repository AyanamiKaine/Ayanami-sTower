defmodule StellaInvictaTest do
  use ExUnit.Case, async: true
  doctest StellaInvicta

  test "luerl hello world" do
    # 1. Initialize the Lua VM state
    lua_state = :luerl.init()

    # 2. Define a simple Lua script
    script = "return 'Hello from Lua!'"

    # 3. Execute the script
    # :luerl.do/2 returns {result, new_state}
    {:ok, result, _} = :luerl.do(script, lua_state)
    assert result == ["Hello from Lua!"]
  end

  test "lua can call an exposed elixir function (Modding API)" do
    # 1. Initialize
    lua_state = :luerl.init()

    # 2. Define an Elixir function to expose
    # It receives arguments as a list and the current state
    multiply_fn = fn [a, b], state ->
      result = a * b
      # Return result to Lua, keep state
      {[result], state}
    end

    # 3. Inject it into Lua as 'elixir_multiply'
    # CORRECTION: Use :luerl.set_table_keys/3 instead of set_table/3.
    # We use a list of strings for the path: ["elixir_multiply"]
    # CRITICAL FIX 1: set_table_keys returns {:ok, state}, so we must unwrap it!
    # CRITICAL FIX 2: We must wrap the function in {:erl_func, fn} so luerl recognizes it as executable code.
    {:ok, lua_state} =
      :luerl.set_table_keys(["elixir_multiply"], {:erl_func, multiply_fn}, lua_state)

    # 4. Run a script that uses this new function
    script = """
    local val = elixir_multiply(10, 5)
    return val
    """

    {:ok, result, _} = :luerl.do(script, lua_state)

    # 5. Verify the calculation happened in Elixir and returned to Lua
    assert result == [50]
  end
end
