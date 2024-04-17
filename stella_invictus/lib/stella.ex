defmodule Stella do
  @moduledoc """
  Documentation for `Stella`.
  """
  use GenServer

  ## Client API

  ## Server Callbacks

  @impl true
  def init(:ok) do
    {:ok, %{current_round: 0}}
  end

  @impl true
  def handle_call(:progress_one_round, _from, state) do
    {:reply, :ok, %{state | current_round: state.current_round + 1}}
  end

  @impl true
  def handle_call(:get_current_round, _from, state) do
    {:reply, state.current_round, state}
  end
end
