defmodule DirectAgeTest do
  use ExUnit.Case

  test "call age system subscriptions directly" do
    IO.puts("\n=== Direct Age System Call ===")

    # Direct call
    subs = StellaInvicta.System.Age.subscriptions()
    IO.puts("Direct call result: #{inspect(subs)}")
    assert subs == [:date_events]
  end

  test "call date system subscriptions directly" do
    IO.puts("\n=== Direct Date System Call ===")

    # Direct call
    subs = StellaInvicta.System.Date.subscriptions()
    IO.puts("Direct call result: #{inspect(subs)}")
    assert subs == []
  end

  test "check function_exported for both" do
    IO.puts("\n=== function_exported checks ===")

    age_exported = function_exported?(StellaInvicta.System.Age, :subscriptions, 0)
    IO.puts("Age subscriptions/0 exported: #{age_exported}")

    date_exported = function_exported?(StellaInvicta.System.Date, :subscriptions, 0)
    IO.puts("Date subscriptions/0 exported: #{date_exported}")
  end

  test "check via System.get_subscriptions" do
    IO.puts("\n=== System.get_subscriptions checks ===")

    age_subs = StellaInvicta.System.get_subscriptions(StellaInvicta.System.Age)
    IO.puts("Age get_subscriptions: #{inspect(age_subs)}")

    date_subs = StellaInvicta.System.get_subscriptions(StellaInvicta.System.Date)
    IO.puts("Date get_subscriptions: #{inspect(date_subs)}")
  end
end
