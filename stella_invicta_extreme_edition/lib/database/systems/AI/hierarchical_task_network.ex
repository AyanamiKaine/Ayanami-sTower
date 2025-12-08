# Hierarchical Task Network (HTN) Planner for Stella Invicta
# A total-order forward decomposition planner for AI decision making

defmodule StellaInvicta.AI.HierarchicalTaskNetwork do
  @moduledoc """
  A Hierarchical Task Network (HTN) planner implementation.

  HTN planning works by decomposing high-level compound tasks into
  primitive tasks that can be directly executed. The planner searches
  through possible decompositions to find a valid plan.

  ## Concepts

  - **Primitive Task**: A directly executable action with preconditions,
    effects, and an operator function.
  - **Compound Task**: A high-level task with multiple methods (ways to
    decompose it into subtasks).
  - **Method**: A decomposition strategy with conditions and subtasks.
  - **Domain**: A collection of all tasks known to the planner.
  - **Operator**: A function that executes a primitive task and returns
    the modified world state.

  ## Performance Notes

  - **Arity**: For performance reasons, all callback functions (preconditions,
    effects, method conditions) MUST accept exactly 2 arguments: `(world, params)`.
    Use `_params` if you don't need the second argument.
  - **Running States**: Operators can return `{:running, new_world}` to indicate
    a task takes multiple ticks to complete.

  ## Example

      # Define a domain
      domain = HTN.Domain.new("character_behavior")
      |> HTN.Domain.add_task(travel_to_location_task())
      |> HTN.Domain.add_task(move_task())

      # Plan
      {:ok, plan} = HTN.Planner.find_plan(domain, world, :travel_to_location)

      # Execute
      {:ok, new_world} = HTN.Planner.execute_plan(plan, world)
  """

  defmodule Task do
    @moduledoc """
    Represents a task in the HTN domain.

    Tasks can be either:
    - Primitive: directly executable with an operator
    - Compound: decomposed into subtasks via methods
    """

    @type task_type :: :primitive | :compound

    @type t :: %__MODULE__{
            name: atom(),
            type: task_type(),
            # For primitive tasks
            preconditions: [(world :: map(), params :: map() -> boolean())] | nil,
            effects: [(world :: map(), params :: map() -> map())] | nil,
            operator:
              (world :: map(), params :: map()
               -> {:ok, map()} | {:running, map()} | {:error, term()})
              | nil,
            # For compound tasks
            methods: [Method.t()] | nil,
            # Optional parameters schema
            params: map()
          }

    defstruct [
      :name,
      :type,
      :preconditions,
      :effects,
      :operator,
      :methods,
      params: %{}
    ]

    @doc """
    Creates a new primitive task.

    ## Options

    - `:preconditions` - List of (world, params) -> bool functions
    - `:effects` - List of (world, params) -> world functions
    - `:operator` - Function that executes the task
    - `:params` - Default parameters for the task
    """
    @spec primitive(atom(), keyword()) :: t()
    def primitive(name, opts \\ []) do
      %__MODULE__{
        name: name,
        type: :primitive,
        preconditions: Keyword.get(opts, :preconditions, []),
        effects: Keyword.get(opts, :effects, []),
        operator: Keyword.get(opts, :operator),
        params: Keyword.get(opts, :params, %{})
      }
    end

    @doc """
    Creates a new compound task.

    ## Options

    - `:methods` - List of Method structs for decomposition
    - `:params` - Default parameters for the task
    """
    @spec compound(atom(), keyword()) :: t()
    def compound(name, opts \\ []) do
      %__MODULE__{
        name: name,
        type: :compound,
        methods: Keyword.get(opts, :methods, []),
        params: Keyword.get(opts, :params, %{})
      }
    end

    @doc """
    Checks if all preconditions for a primitive task are satisfied.
    Assuming strict arity-2 functions for performance.
    """
    @spec preconditions_met?(t(), map(), map()) :: boolean()
    def preconditions_met?(%__MODULE__{preconditions: nil}, _world, _params), do: true
    def preconditions_met?(%__MODULE__{preconditions: []}, _world, _params), do: true

    def preconditions_met?(%__MODULE__{preconditions: conditions}, world, params) do
      Enum.all?(conditions, fn condition -> condition.(world, params) end)
    end

    @doc """
    Applies the effects of a primitive task to the world state.
    Used during planning to simulate task execution.
    Assuming strict arity-2 functions for performance.
    """
    @spec apply_effects(t(), map(), map()) :: map()
    def apply_effects(%__MODULE__{effects: nil}, world, _params), do: world
    def apply_effects(%__MODULE__{effects: []}, world, _params), do: world

    def apply_effects(%__MODULE__{effects: effects}, world, params) do
      Enum.reduce(effects, world, fn effect, acc -> effect.(acc, params) end)
    end
  end

  defmodule Method do
    @moduledoc """
    Represents a method for decomposing a compound task.

    Methods have conditions that determine when they're applicable,
    and a list of subtasks that replace the compound task when the
    method is chosen.
    """

    @type t :: %__MODULE__{
            name: atom(),
            priority: integer(),
            conditions: [(world :: map(), params :: map() -> boolean())],
            subtasks: [{atom(), map()}]
          }

    defstruct [
      :name,
      priority: 0,
      conditions: [],
      subtasks: []
    ]

    @doc """
    Creates a new method.

    ## Options

    - `:priority` - Higher priority methods are tried first (default: 0)
    - `:conditions` - List of (world, params) -> bool functions
    - `:subtasks` - List of {task_name, params} tuples
    """
    @spec new(atom(), keyword()) :: t()
    def new(name, opts \\ []) do
      %__MODULE__{
        name: name,
        priority: Keyword.get(opts, :priority, 0),
        conditions: Keyword.get(opts, :conditions, []),
        subtasks: Keyword.get(opts, :subtasks, [])
      }
    end

    @doc """
    Checks if all conditions for this method are satisfied.
    Assuming strict arity-2 functions for performance.
    """
    @spec applicable?(t(), map(), map()) :: boolean()
    def applicable?(%__MODULE__{conditions: []}, _world, _params), do: true

    def applicable?(%__MODULE__{conditions: conditions}, world, params) do
      Enum.all?(conditions, fn condition -> condition.(world, params) end)
    end
  end

  defmodule Domain do
    @moduledoc """
    A domain is a collection of all tasks known to the HTN planner.
    """

    @type t :: %__MODULE__{
            name: String.t(),
            tasks: %{atom() => Task.t()}
          }

    defstruct [
      :name,
      tasks: %{}
    ]

    @doc """
    Creates a new empty domain.
    """
    @spec new(String.t()) :: t()
    def new(name) do
      %__MODULE__{name: name, tasks: %{}}
    end

    @doc """
    Adds a task to the domain.
    """
    @spec add_task(t(), Task.t()) :: t()
    def add_task(%__MODULE__{} = domain, %Task{} = task) do
      %{domain | tasks: Map.put(domain.tasks, task.name, task)}
    end

    @doc """
    Gets a task from the domain by name.
    """
    @spec get_task(t(), atom()) :: {:ok, Task.t()} | {:error, :task_not_found}
    def get_task(%__MODULE__{tasks: tasks}, name) do
      case Map.get(tasks, name) do
        nil -> {:error, :task_not_found}
        task -> {:ok, task}
      end
    end

    @doc """
    Lists all task names in the domain.
    """
    @spec list_tasks(t()) :: [atom()]
    def list_tasks(%__MODULE__{tasks: tasks}) do
      Map.keys(tasks)
    end

    @doc """
    Validates the domain structure.
    Checks that all compound task methods refer to subtasks that actually exist in the domain.
    Useful to run at application startup to catch typos.
    """
    @spec validate(t()) :: :ok | {:error, [String.t()]}
    def validate(%__MODULE__{tasks: tasks}) do
      errors =
        Enum.reduce(tasks, [], fn {task_name, task}, errors ->
          if task.type == :compound do
            # Check all methods
            Enum.reduce(task.methods, errors, fn method, acc ->
              Enum.reduce(method.subtasks, acc, fn {subtask_name, _}, inner_acc ->
                if Map.has_key?(tasks, subtask_name) do
                  inner_acc
                else
                  [
                    "Task :#{task_name} (Method :#{method.name}) refers to unknown subtask :#{subtask_name}"
                    | inner_acc
                  ]
                end
              end)
            end)
          else
            errors
          end
        end)

      if errors == [], do: :ok, else: {:error, errors}
    end
  end

  defmodule Plan do
    @moduledoc """
    Represents a generated plan - a sequence of primitive tasks to execute.
    """

    @type step :: {atom(), map()}

    @type t :: %__MODULE__{
            steps: [step()],
            status: :pending | :executing | :completed | :failed,
            current_step: non_neg_integer()
          }

    defstruct steps: [],
              status: :pending,
              current_step: 0

    @doc """
    Creates a new plan from a list of steps.
    """
    @spec new([step()]) :: t()
    def new(steps) do
      %__MODULE__{steps: steps, status: :pending, current_step: 0}
    end

    @doc """
    Returns the number of steps in the plan.
    """
    @spec length(t()) :: non_neg_integer()
    def length(%__MODULE__{steps: steps}), do: Kernel.length(steps)

    @doc """
    Checks if the plan is empty.
    """
    @spec empty?(t()) :: boolean()
    def empty?(%__MODULE__{steps: []}), do: true
    def empty?(_), do: false

    @doc """
    Gets the current step to execute.
    """
    @spec current(t()) :: step() | nil
    def current(%__MODULE__{steps: steps, current_step: idx}) do
      Enum.at(steps, idx)
    end

    @doc """
    Advances to the next step.
    """
    @spec advance(t()) :: t()
    def advance(%__MODULE__{steps: steps, current_step: idx} = plan) do
      new_idx = idx + 1

      if new_idx >= Kernel.length(steps) do
        %{plan | current_step: new_idx, status: :completed}
      else
        %{plan | current_step: new_idx, status: :executing}
      end
    end

    @doc """
    Marks the plan as failed.
    """
    @spec fail(t()) :: t()
    def fail(plan), do: %{plan | status: :failed}

    @doc """
    Checks if the plan is complete.
    """
    @spec complete?(t()) :: boolean()
    def complete?(%__MODULE__{status: :completed}), do: true
    def complete?(_), do: false
  end

  defmodule Planner do
    @moduledoc """
    The HTN planner that generates and executes plans.

    Uses forward decomposition: starts from the current world state
    and decomposes tasks until only primitive tasks remain.
    """

    alias StellaInvicta.AI.HierarchicalTaskNetwork.{Task, Method, Domain, Plan}

    @type planning_result :: {:ok, Plan.t()} | {:error, term()}
    @type execution_result :: {:ok, map()} | {:error, term(), map()}

    @doc """
    Finds a plan to accomplish the given goal task.

    ## Options

    - `:max_iterations` - Maximum planning iterations (default: 1000)
    - `:max_depth` - Maximum recursion depth for task decomposition (default: 50)
    - `:params` - Initial parameters for the root task
    """
    @spec find_plan(Domain.t(), map(), atom(), keyword()) :: planning_result()
    def find_plan(domain, world, root_task, opts \\ []) do
      max_iterations = Keyword.get(opts, :max_iterations, 1000)
      max_depth = Keyword.get(opts, :max_depth, 50)
      params = Keyword.get(opts, :params, %{})

      initial_state = %{
        tasks_to_process: [{root_task, params}],
        final_plan: [],
        working_world: world,
        decomposition_history: [],
        iterations: 0
      }

      case do_plan(domain, initial_state, max_iterations, max_depth) do
        {:ok, steps} -> {:ok, Plan.new(steps)}
        {:error, _} = error -> error
      end
    end

    @doc """
    Finds a plan with metrics tracking enabled.

    Returns `{:ok, plan, metrics}` or `{:error, reason, metrics}`.
    """
    alias StellaInvicta.AI.HierarchicalTaskNetwork.Metrics

    @spec find_plan_with_metrics(Domain.t(), map(), atom(), Metrics.t(), keyword()) ::
            {:ok, Plan.t(), Metrics.t()} | {:error, term(), Metrics.t()}
    def find_plan_with_metrics(domain, world, root_task, metrics, opts \\ []) do
      max_iterations = Keyword.get(opts, :max_iterations, 1000)
      max_depth = Keyword.get(opts, :max_depth, 50)
      params = Keyword.get(opts, :params, %{})

      start_time = System.monotonic_time(:microsecond)
      metrics = Metrics.start_planning(metrics, root_task, params)

      initial_state = %{
        tasks_to_process: [{root_task, params}],
        final_plan: [],
        working_world: world,
        decomposition_history: [],
        iterations: 0,
        metrics: metrics
      }

      case do_plan_with_metrics(domain, initial_state, max_iterations, max_depth) do
        {:ok, steps, updated_metrics} ->
          elapsed = System.monotonic_time(:microsecond) - start_time
          plan = Plan.new(steps)
          final_metrics = Metrics.plan_found(updated_metrics, length(steps), elapsed)
          {:ok, plan, final_metrics}

        {:error, reason, updated_metrics} ->
          elapsed = System.monotonic_time(:microsecond) - start_time
          final_metrics = Metrics.plan_failed(updated_metrics, reason, elapsed)
          {:error, reason, final_metrics}
      end
    end

    defp do_plan(_domain, %{tasks_to_process: [], final_plan: plan}, _max_iter, _max_depth) do
      {:ok, Enum.reverse(plan)}
    end

    defp do_plan(_domain, %{iterations: iter}, max_iter, _max_depth) when iter >= max_iter do
      {:error, :max_iterations_exceeded}
    end

    defp do_plan(domain, state, max_iterations, max_depth) do
      %{
        tasks_to_process: [{task_name, params} | rest],
        final_plan: plan,
        working_world: world,
        decomposition_history: history,
        iterations: iter
      } = state

      # Depth Check
      if length(history) > max_depth do
        {:error, :max_depth_exceeded}
      else
        case Domain.get_task(domain, task_name) do
          {:error, :task_not_found} ->
            {:error, {:task_not_found, task_name}}

          {:ok, %Task{type: :primitive} = task} ->
            if Task.preconditions_met?(task, world, params) do
              new_world = Task.apply_effects(task, world, params)

              new_state = %{
                tasks_to_process: rest,
                final_plan: [{task_name, params} | plan],
                working_world: new_world,
                decomposition_history: history,
                iterations: iter + 1
              }

              do_plan(domain, new_state, max_iterations, max_depth)
            else
              case backtrack(domain, history, max_iterations) do
                {:continue, new_state} -> do_plan(domain, new_state, max_iterations, max_depth)
                {:error, _} = error -> error
              end
            end

          {:ok, %Task{type: :compound, methods: methods} = _task} ->
            sorted_methods = Enum.sort_by(methods, & &1.priority, :desc)

            case find_applicable_method(sorted_methods, world, params) do
              {:ok, method, method_index} ->
                history_entry = %{
                  task: {task_name, params},
                  remaining_tasks: rest,
                  method_index: method_index,
                  world_snapshot: world,
                  plan_state: plan
                }

                new_tasks =
                  Enum.map(method.subtasks, fn {subtask_name, subtask_params} ->
                    {subtask_name, Map.merge(params, subtask_params)}
                  end)

                new_state = %{
                  tasks_to_process: new_tasks ++ rest,
                  final_plan: plan,
                  working_world: world,
                  decomposition_history: [history_entry | history],
                  iterations: iter + 1
                }

                do_plan(domain, new_state, max_iterations, max_depth)

              {:error, :no_applicable_method} ->
                case backtrack(domain, history, max_iterations) do
                  {:continue, new_state} -> do_plan(domain, new_state, max_iterations, max_depth)
                  {:error, _} = error -> error
                end
            end
        end
      end
    end

    defp find_applicable_method(methods, world, params) do
      methods
      |> Enum.with_index()
      |> Enum.find_value(fn {method, index} ->
        if Method.applicable?(method, world, params) do
          {:ok, method, index}
        end
      end)
      |> case do
        nil -> {:error, :no_applicable_method}
        result -> result
      end
    end

    defp backtrack(_domain, [], _max_iterations) do
      {:error, :no_plan_found}
    end

    defp backtrack(domain, [entry | rest_history], _max_iterations) do
      %{
        task: {task_name, params},
        remaining_tasks: remaining,
        method_index: tried_index,
        world_snapshot: world,
        plan_state: plan
      } = entry

      case Domain.get_task(domain, task_name) do
        {:ok, %Task{methods: methods}} ->
          sorted_methods = Enum.sort_by(methods, & &1.priority, :desc)
          remaining_methods = Enum.drop(sorted_methods, tried_index + 1)

          case find_applicable_method_from(remaining_methods, world, params, tried_index + 1) do
            {:ok, method, new_index} ->
              new_history_entry = %{
                task: {task_name, params},
                remaining_tasks: remaining,
                method_index: new_index,
                world_snapshot: world,
                plan_state: plan
              }

              new_tasks =
                Enum.map(method.subtasks, fn {subtask_name, subtask_params} ->
                  {subtask_name, Map.merge(params, subtask_params)}
                end)

              new_state = %{
                tasks_to_process: new_tasks ++ remaining,
                final_plan: plan,
                working_world: world,
                decomposition_history: [new_history_entry | rest_history],
                iterations: 0
              }

              {:continue, new_state}

            {:error, :no_applicable_method} ->
              backtrack(domain, rest_history, 0)
          end

        _ ->
          backtrack(domain, rest_history, 0)
      end
    end

    defp find_applicable_method_from(methods, world, params, base_index) do
      methods
      |> Enum.with_index(base_index)
      |> Enum.find_value(fn {method, index} ->
        if Method.applicable?(method, world, params) do
          {:ok, method, index}
        end
      end)
      |> case do
        nil -> {:error, :no_applicable_method}
        result -> result
      end
    end

    # --- Metrics-enabled planning ---

    defp do_plan_with_metrics(
           _domain,
           %{tasks_to_process: [], final_plan: plan, metrics: metrics},
           _max,
           _depth
         ) do
      {:ok, Enum.reverse(plan), metrics}
    end

    defp do_plan_with_metrics(_domain, %{iterations: iter, metrics: metrics}, max, _depth)
         when iter >= max do
      {:error, :max_iterations_exceeded, metrics}
    end

    defp do_plan_with_metrics(domain, state, max_iterations, max_depth) do
      %{
        tasks_to_process: [{task_name, params} | rest],
        final_plan: plan,
        working_world: world,
        decomposition_history: history,
        iterations: iter,
        metrics: metrics
      } = state

      if length(history) > max_depth do
        {:error, :max_depth_exceeded, metrics}
      else
        case Domain.get_task(domain, task_name) do
          {:error, :task_not_found} ->
            {:error, {:task_not_found, task_name}, metrics}

          {:ok, %Task{type: :primitive} = task} ->
            if Task.preconditions_met?(task, world, params) do
              new_world = Task.apply_effects(task, world, params)
              updated_metrics = Metrics.primitive_added(metrics, task_name, params, world)

              new_state = %{
                tasks_to_process: rest,
                final_plan: [{task_name, params} | plan],
                working_world: new_world,
                decomposition_history: history,
                iterations: iter + 1,
                metrics: updated_metrics
              }

              do_plan_with_metrics(domain, new_state, max_iterations, max_depth)
            else
              updated_metrics = Metrics.primitive_failed(metrics, task_name, params, world)

              case backtrack_with_metrics(domain, history, max_iterations, updated_metrics) do
                {:continue, new_state} ->
                  do_plan_with_metrics(domain, new_state, max_iterations, max_depth)

                {:error, reason, final_metrics} ->
                  {:error, reason, final_metrics}
              end
            end

          {:ok, %Task{type: :compound, methods: methods} = _task} ->
            sorted_methods = Enum.sort_by(methods, & &1.priority, :desc)

            case find_applicable_method_with_metrics(
                   sorted_methods,
                   world,
                   params,
                   metrics,
                   task_name
                 ) do
              {:ok, method, method_index, updated_metrics} ->
                history_entry = %{
                  task: {task_name, params},
                  remaining_tasks: rest,
                  method_index: method_index,
                  world_snapshot: world,
                  plan_state: plan
                }

                new_tasks =
                  Enum.map(method.subtasks, fn {subtask_name, subtask_params} ->
                    {subtask_name, Map.merge(params, subtask_params)}
                  end)

                new_state = %{
                  tasks_to_process: new_tasks ++ rest,
                  final_plan: plan,
                  working_world: world,
                  decomposition_history: [history_entry | history],
                  iterations: iter + 1,
                  metrics: updated_metrics
                }

                do_plan_with_metrics(domain, new_state, max_iterations, max_depth)

              {:error, :no_applicable_method, updated_metrics} ->
                case backtrack_with_metrics(domain, history, max_iterations, updated_metrics) do
                  {:continue, new_state} ->
                    do_plan_with_metrics(domain, new_state, max_iterations, max_depth)

                  {:error, reason, final_metrics} ->
                    {:error, reason, final_metrics}
                end
            end
        end
      end
    end

    defp find_applicable_method_with_metrics(methods, world, params, metrics, task_name) do
      methods
      |> Enum.with_index()
      |> Enum.reduce_while({:error, :no_applicable_method, metrics}, fn {method, index},
                                                                        {_, _, acc_metrics} ->
        if Method.applicable?(method, world, params) do
          updated_metrics =
            Metrics.method_selected(acc_metrics, task_name, method.name, params, world)

          {:halt, {:ok, method, index, updated_metrics}}
        else
          updated_metrics =
            Metrics.method_rejected(acc_metrics, task_name, method.name, :conditions_not_met)

          {:cont, {:error, :no_applicable_method, updated_metrics}}
        end
      end)
    end

    defp backtrack_with_metrics(_domain, [], _max_iterations, metrics) do
      {:error, :no_plan_found, metrics}
    end

    defp backtrack_with_metrics(domain, [entry | rest_history], _max_iterations, metrics) do
      %{
        task: {task_name, params},
        remaining_tasks: remaining,
        method_index: tried_index,
        world_snapshot: world,
        plan_state: plan
      } = entry

      case Domain.get_task(domain, task_name) do
        {:ok, %Task{methods: methods}} ->
          sorted_methods = Enum.sort_by(methods, & &1.priority, :desc)
          tried_method = Enum.at(sorted_methods, tried_index)
          tried_method_name = if tried_method, do: tried_method.name, else: :unknown

          updated_metrics =
            Metrics.backtrack(metrics, task_name, tried_method_name, :trying_next_method)

          remaining_methods = Enum.drop(sorted_methods, tried_index + 1)

          case find_applicable_method_from_with_metrics(
                 remaining_methods,
                 world,
                 params,
                 tried_index + 1,
                 updated_metrics,
                 task_name
               ) do
            {:ok, method, new_index, final_metrics} ->
              new_history_entry = %{
                task: {task_name, params},
                remaining_tasks: remaining,
                method_index: new_index,
                world_snapshot: world,
                plan_state: plan
              }

              new_tasks =
                Enum.map(method.subtasks, fn {subtask_name, subtask_params} ->
                  {subtask_name, Map.merge(params, subtask_params)}
                end)

              new_state = %{
                tasks_to_process: new_tasks ++ remaining,
                final_plan: plan,
                working_world: world,
                decomposition_history: [new_history_entry | rest_history],
                iterations: 0,
                metrics: final_metrics
              }

              {:continue, new_state}

            {:error, :no_applicable_method, final_metrics} ->
              backtrack_with_metrics(domain, rest_history, 0, final_metrics)
          end

        _ ->
          backtrack_with_metrics(domain, rest_history, 0, metrics)
      end
    end

    defp find_applicable_method_from_with_metrics(
           methods,
           world,
           params,
           base_index,
           metrics,
           task_name
         ) do
      methods
      |> Enum.with_index(base_index)
      |> Enum.reduce_while({:error, :no_applicable_method, metrics}, fn {method, index},
                                                                        {_, _, acc_metrics} ->
        if Method.applicable?(method, world, params) do
          updated_metrics =
            Metrics.method_selected(acc_metrics, task_name, method.name, params, world)

          {:halt, {:ok, method, index, updated_metrics}}
        else
          updated_metrics =
            Metrics.method_rejected(acc_metrics, task_name, method.name, :conditions_not_met)

          {:cont, {:error, :no_applicable_method, updated_metrics}}
        end
      end)
    end

    @doc """
    Executes a plan step by step, modifying the world state.

    Returns the final world state after all steps are executed,
    or an error if any step fails.
    """
    @spec execute_plan(Plan.t(), Domain.t(), map()) :: execution_result()
    def execute_plan(%Plan{steps: steps}, domain, world) do
      Enum.reduce_while(steps, {:ok, world}, fn {task_name, params}, {:ok, current_world} ->
        case Domain.get_task(domain, task_name) do
          {:ok, %Task{type: :primitive, operator: operator}} when not is_nil(operator) ->
            case operator.(current_world, params) do
              {:ok, new_world} -> {:cont, {:ok, new_world}}
              {:running, new_world} -> {:cont, {:ok, new_world}}
              {:error, reason} -> {:halt, {:error, reason, current_world}}
            end

          {:ok, %Task{type: :primitive, operator: nil} = task} ->
            # No operator, just apply effects
            new_world = Task.apply_effects(task, current_world, params)
            {:cont, {:ok, new_world}}

          {:error, :task_not_found} ->
            {:halt, {:error, {:task_not_found, task_name}, current_world}}

          {:ok, %Task{type: :compound}} ->
            {:halt, {:error, {:compound_task_in_plan, task_name}, current_world}}
        end
      end)
    end

    @doc """
    Executes a single step of the plan and returns the updated plan and world.
    Useful for incremental execution over multiple game ticks.

    If an operator returns `{:running, world}`, the plan is NOT advanced,
    allowing the same task to continue execution on the next tick.
    """
    @spec execute_step(Plan.t(), Domain.t(), map()) ::
            {:ok, Plan.t(), map()}
            | {:running, Plan.t(), map()}
            | {:complete, map()}
            | {:error, term(), Plan.t(), map()}
    def execute_step(%Plan{status: :completed} = _plan, _domain, world) do
      {:complete, world}
    end

    def execute_step(%Plan{} = plan, domain, world) do
      case Plan.current(plan) do
        nil ->
          {:complete, world}

        {task_name, params} ->
          case Domain.get_task(domain, task_name) do
            {:ok, %Task{type: :primitive, operator: operator}} when not is_nil(operator) ->
              case operator.(world, params) do
                {:ok, new_world} ->
                  new_plan = Plan.advance(plan)
                  {:ok, new_plan, new_world}

                {:running, new_world} ->
                  # Task is still running, don't advance plan
                  {:running, plan, new_world}

                {:error, reason} ->
                  {:error, reason, Plan.fail(plan), world}
              end

            {:ok, %Task{type: :primitive, operator: nil} = task} ->
              new_world = Task.apply_effects(task, world, params)
              new_plan = Plan.advance(plan)
              {:ok, new_plan, new_world}

            {:error, :task_not_found} ->
              {:error, {:task_not_found, task_name}, Plan.fail(plan), world}

            {:ok, %Task{type: :compound}} ->
              {:error, {:compound_task_in_plan, task_name}, Plan.fail(plan), world}
          end
      end
    end

    # --- Metrics-enabled execution ---

    @doc """
    Executes a plan with metrics tracking, recording each step's execution.
    """
    @spec execute_plan_with_metrics(Plan.t(), Domain.t(), map(), Metrics.t()) ::
            {:ok, map(), Metrics.t()} | {:error, term(), map(), Metrics.t()}
    def execute_plan_with_metrics(%Plan{steps: steps}, domain, world, metrics) do
      initial_metrics = Metrics.execution_started(metrics, Enum.count(steps))

      Enum.reduce_while(steps, {:ok, world, initial_metrics}, fn {task_name, params},
                                                                 {:ok, current_world, acc_metrics} ->
        case Domain.get_task(domain, task_name) do
          {:ok, %Task{type: :primitive, operator: operator}} when not is_nil(operator) ->
            case operator.(current_world, params) do
              {:ok, new_world} ->
                updated_metrics = Metrics.task_executed(acc_metrics, task_name, params, :success)
                {:cont, {:ok, new_world, updated_metrics}}

              {:running, new_world} ->
                # For execute_plan (bulk), we treat running as success for that step conceptually,
                # but technically execute_plan shouldn't be used with long-running tasks.
                # We'll treat it as a success/continue for now.
                updated_metrics = Metrics.task_executed(acc_metrics, task_name, params, :running)
                {:cont, {:ok, new_world, updated_metrics}}

              {:error, reason} ->
                updated_metrics =
                  Metrics.task_executed(acc_metrics, task_name, params, {:failure, reason})

                {:halt, {:error, reason, current_world, updated_metrics}}
            end

          {:ok, %Task{type: :primitive, operator: nil} = task} ->
            new_world = Task.apply_effects(task, current_world, params)
            updated_metrics = Metrics.task_executed(acc_metrics, task_name, params, :success)
            {:cont, {:ok, new_world, updated_metrics}}

          {:error, :task_not_found} ->
            updated_metrics =
              Metrics.task_executed(acc_metrics, task_name, params, {:failure, :task_not_found})

            {:halt, {:error, {:task_not_found, task_name}, current_world, updated_metrics}}

          {:ok, %Task{type: :compound}} ->
            updated_metrics =
              Metrics.task_executed(acc_metrics, task_name, params, {:failure, :compound_in_plan})

            {:halt, {:error, {:compound_task_in_plan, task_name}, current_world, updated_metrics}}
        end
      end)
    end

    @doc """
    Executes a single step with metrics tracking.
    """
    @spec execute_step_with_metrics(Plan.t(), Domain.t(), map(), Metrics.t()) ::
            {:ok, Plan.t(), map(), Metrics.t()}
            | {:running, Plan.t(), map(), Metrics.t()}
            | {:complete, map(), Metrics.t()}
            | {:error, term(), Plan.t(), map(), Metrics.t()}
    def execute_step_with_metrics(%Plan{status: :completed} = _plan, _domain, world, metrics) do
      {:complete, world, metrics}
    end

    def execute_step_with_metrics(%Plan{} = plan, domain, world, metrics) do
      case Plan.current(plan) do
        nil ->
          {:complete, world, metrics}

        {task_name, params} ->
          case Domain.get_task(domain, task_name) do
            {:ok, %Task{type: :primitive, operator: operator}} when not is_nil(operator) ->
              case operator.(world, params) do
                {:ok, new_world} ->
                  updated_metrics = Metrics.task_executed(metrics, task_name, params, :success)
                  new_plan = Plan.advance(plan)
                  {:ok, new_plan, new_world, updated_metrics}

                {:running, new_world} ->
                  updated_metrics = Metrics.task_executed(metrics, task_name, params, :running)
                  {:running, plan, new_world, updated_metrics}

                {:error, reason} ->
                  updated_metrics =
                    Metrics.task_executed(metrics, task_name, params, {:failure, reason})

                  {:error, reason, Plan.fail(plan), world, updated_metrics}
              end

            {:ok, %Task{type: :primitive, operator: nil} = task} ->
              new_world = Task.apply_effects(task, world, params)
              updated_metrics = Metrics.task_executed(metrics, task_name, params, :success)
              new_plan = Plan.advance(plan)
              {:ok, new_plan, new_world, updated_metrics}

            {:error, :task_not_found} ->
              updated_metrics =
                Metrics.task_executed(metrics, task_name, params, {:failure, :task_not_found})

              {:error, {:task_not_found, task_name}, Plan.fail(plan), world, updated_metrics}

            {:ok, %Task{type: :compound}} ->
              updated_metrics =
                Metrics.task_executed(metrics, task_name, params, {:failure, :compound_in_plan})

              {:error, {:compound_task_in_plan, task_name}, Plan.fail(plan), world,
               updated_metrics}
          end
      end
    end
  end

  # Convenience aliases
  defdelegate primitive(name, opts \\ []), to: Task
  defdelegate compound(name, opts \\ []), to: Task
  defdelegate method(name, opts \\ []), to: Method, as: :new
  defdelegate new_domain(name), to: Domain, as: :new
  defdelegate add_task(domain, task), to: Domain
  defdelegate validate_domain(domain), to: Domain, as: :validate
  defdelegate find_plan(domain, world, task, opts \\ []), to: Planner
  defdelegate execute_plan(plan, domain, world), to: Planner
  defdelegate execute_step(plan, domain, world), to: Planner

  # Metrics-enabled functions
  defdelegate find_plan_with_metrics(domain, world, task, metrics, opts \\ []), to: Planner
  defdelegate execute_plan_with_metrics(plan, domain, world, metrics), to: Planner
  defdelegate execute_step_with_metrics(plan, domain, world, metrics), to: Planner

  # Metrics delegation
  defdelegate new_metrics(), to: __MODULE__.Metrics, as: :new
  defdelegate new_metrics(opts), to: __MODULE__.Metrics, as: :new
  defdelegate get_decision_log(metrics), to: __MODULE__.Metrics
  defdelegate get_planning_summary(metrics), to: __MODULE__.Metrics
  defdelegate clear_metrics(metrics), to: __MODULE__.Metrics, as: :clear
  defdelegate get_formatted_log(metrics, opts \\ []), to: __MODULE__.Metrics

  defmodule Metrics do
    @moduledoc """
    Metrics and decision logging for HTN planning.
    Tracks planning decisions, method selections, backtracking events,
    and execution details to help debug and understand AI behavior.
    """

    @type decision_type ::
            :task_started
            | :primitive_success
            | :primitive_failed
            | :compound_decomposed
            | :method_selected
            | :method_rejected
            | :backtrack
            | :plan_found
            | :plan_failed
            | :execution_started
            | :execution_step
            | :execution_complete
            | :execution_failed

    @type decision_entry :: %{
            timestamp: integer(),
            type: decision_type(),
            task: atom() | nil,
            method: atom() | nil,
            reason: term() | nil,
            params: map(),
            world_snapshot: map() | nil,
            details: map()
          }

    @type t :: %__MODULE__{
            decisions: [decision_entry()],
            decision_count: non_neg_integer(),
            planning_attempts: non_neg_integer(),
            successful_plans: non_neg_integer(),
            failed_plans: non_neg_integer(),
            total_backtrack_count: non_neg_integer(),
            total_iterations: non_neg_integer(),
            planning_time_us: non_neg_integer(),
            method_selection_counts: %{atom() => non_neg_integer()},
            task_execution_counts: %{atom() => non_neg_integer()},
            enabled: boolean(),
            capture_world_snapshots: boolean(),
            max_decisions: non_neg_integer()
          }

    defstruct decisions: [],
              decision_count: 0,
              planning_attempts: 0,
              successful_plans: 0,
              failed_plans: 0,
              total_backtrack_count: 0,
              total_iterations: 0,
              planning_time_us: 0,
              method_selection_counts: %{},
              task_execution_counts: %{},
              enabled: true,
              capture_world_snapshots: false,
              max_decisions: 1000

    @doc """
    Creates a new metrics tracker.
    """
    @spec new(keyword()) :: t()
    def new(opts \\ []) do
      %__MODULE__{
        enabled: Keyword.get(opts, :enabled, true),
        capture_world_snapshots: Keyword.get(opts, :capture_world_snapshots, false),
        max_decisions: Keyword.get(opts, :max_decisions, 1000)
      }
    end

    @doc """
    Clears all recorded metrics while preserving settings.
    """
    @spec clear(t()) :: t()
    def clear(%__MODULE__{} = metrics) do
      %{
        metrics
        | decisions: [],
          decision_count: 0,
          planning_attempts: 0,
          successful_plans: 0,
          failed_plans: 0,
          total_backtrack_count: 0,
          total_iterations: 0,
          planning_time_us: 0,
          method_selection_counts: %{},
          task_execution_counts: %{}
      }
    end

    @doc """
    Records a planning decision.
    Optimized to avoid O(N) length checks on every insertion.
    Trimming occurs probabilistically every 100 decisions.
    """
    @spec record_decision(t(), decision_type(), keyword()) :: t()
    def record_decision(%__MODULE__{enabled: false} = metrics, _type, _opts), do: metrics

    def record_decision(%__MODULE__{} = metrics, type, opts) do
      entry = %{
        timestamp: System.monotonic_time(:microsecond),
        type: type,
        task: Keyword.get(opts, :task),
        method: Keyword.get(opts, :method),
        reason: Keyword.get(opts, :reason),
        params: Keyword.get(opts, :params, %{}),
        world_snapshot:
          if(metrics.capture_world_snapshots,
            do: Keyword.get(opts, :world),
            else: nil
          ),
        details: Keyword.get(opts, :details, %{})
      }

      new_count = metrics.decision_count + 1
      decisions = [entry | metrics.decisions]

      # Probabilistic trimming: Only check length every 100 inserts
      if rem(new_count, 100) == 0 and new_count > metrics.max_decisions do
        trimmed_decisions = Enum.take(decisions, metrics.max_decisions)
        %{metrics | decisions: trimmed_decisions, decision_count: metrics.max_decisions}
      else
        %{metrics | decisions: decisions, decision_count: new_count}
      end
    end

    @doc """
    Records start of a planning attempt.
    """
    @spec start_planning(t(), atom(), map()) :: t()
    def start_planning(%__MODULE__{enabled: false} = metrics, _task, _params), do: metrics

    def start_planning(%__MODULE__{} = metrics, root_task, params) do
      metrics
      |> Map.update!(:planning_attempts, &(&1 + 1))
      |> record_decision(:task_started, task: root_task, params: params)
    end

    @doc """
    Records successful plan completion.
    """
    @spec plan_found(t(), non_neg_integer(), non_neg_integer()) :: t()
    def plan_found(%__MODULE__{enabled: false} = metrics, _steps, _time), do: metrics

    def plan_found(%__MODULE__{} = metrics, plan_steps, time_us) do
      metrics
      |> Map.update!(:successful_plans, &(&1 + 1))
      |> Map.update!(:planning_time_us, &(&1 + time_us))
      |> record_decision(:plan_found,
        details: %{
          plan_length: plan_steps,
          planning_time_us: time_us,
          iterations: metrics.total_iterations,
          backtracks: metrics.total_backtrack_count
        }
      )
    end

    @doc """
    Records failed planning attempt.
    """
    @spec plan_failed(t(), term(), non_neg_integer()) :: t()
    def plan_failed(%__MODULE__{enabled: false} = metrics, _reason, _time), do: metrics

    def plan_failed(%__MODULE__{} = metrics, reason, time_us) do
      metrics
      |> Map.update!(:failed_plans, &(&1 + 1))
      |> Map.update!(:planning_time_us, &(&1 + time_us))
      |> record_decision(:plan_failed,
        reason: reason,
        details: %{
          planning_time_us: time_us,
          iterations: metrics.total_iterations,
          backtracks: metrics.total_backtrack_count
        }
      )
    end

    @doc """
    Records a primitive task being added to the plan.
    """
    @spec primitive_added(t(), atom(), map(), map() | nil) :: t()
    def primitive_added(%__MODULE__{enabled: false} = metrics, _task, _params, _world),
      do: metrics

    def primitive_added(%__MODULE__{} = metrics, task_name, params, world) do
      metrics
      |> Map.update!(:total_iterations, &(&1 + 1))
      |> update_task_count(task_name)
      |> record_decision(:primitive_success, task: task_name, params: params, world: world)
    end

    @doc """
    Records a primitive task failing preconditions.
    """
    @spec primitive_failed(t(), atom(), map(), map() | nil) :: t()
    def primitive_failed(%__MODULE__{enabled: false} = metrics, _task, _params, _world),
      do: metrics

    def primitive_failed(%__MODULE__{} = metrics, task_name, params, world) do
      metrics
      |> Map.update!(:total_iterations, &(&1 + 1))
      |> record_decision(:primitive_failed,
        task: task_name,
        params: params,
        world: world,
        reason: :preconditions_not_met
      )
    end

    @doc """
    Records a method being selected for a compound task.
    """
    @spec method_selected(t(), atom(), atom(), map(), map() | nil) :: t()
    def method_selected(%__MODULE__{enabled: false} = metrics, _task, _method, _params, _world),
      do: metrics

    def method_selected(%__MODULE__{} = metrics, task_name, method_name, params, world) do
      metrics
      |> Map.update!(:total_iterations, &(&1 + 1))
      |> update_method_count(method_name)
      |> record_decision(:method_selected,
        task: task_name,
        method: method_name,
        params: params,
        world: world
      )
    end

    @doc """
    Records a method being rejected (conditions not met).
    """
    @spec method_rejected(t(), atom(), atom(), term()) :: t()
    def method_rejected(%__MODULE__{enabled: false} = metrics, _task, _method, _reason),
      do: metrics

    def method_rejected(%__MODULE__{} = metrics, task_name, method_name, reason) do
      record_decision(metrics, :method_rejected,
        task: task_name,
        method: method_name,
        reason: reason
      )
    end

    @doc """
    Records a backtrack event.
    """
    @spec backtrack(t(), atom(), atom(), term()) :: t()
    def backtrack(%__MODULE__{enabled: false} = metrics, _task, _method, _reason), do: metrics

    def backtrack(%__MODULE__{} = metrics, task_name, from_method, reason) do
      metrics
      |> Map.update!(:total_backtrack_count, &(&1 + 1))
      |> record_decision(:backtrack,
        task: task_name,
        method: from_method,
        reason: reason,
        details: %{backtrack_number: metrics.total_backtrack_count + 1}
      )
    end

    @doc """
    Records execution of a plan step.
    """
    @spec execution_step(t(), atom(), map(), :success | {:error, term()}) :: t()
    def execution_step(%__MODULE__{enabled: false} = metrics, _task, _params, _result),
      do: metrics

    def execution_step(%__MODULE__{} = metrics, task_name, params, result) do
      metrics
      |> update_task_count(task_name)
      |> record_decision(:execution_step,
        task: task_name,
        params: params,
        details: %{result: result}
      )
    end

    @doc """
    Records the start of plan execution.
    """
    @spec execution_started(t(), non_neg_integer()) :: t()
    def execution_started(%__MODULE__{enabled: false} = metrics, _total_steps), do: metrics

    def execution_started(%__MODULE__{} = metrics, total_steps) do
      record_decision(metrics, :execution_started,
        details: %{total_steps: total_steps, start_time: System.monotonic_time(:microsecond)}
      )
    end

    @doc """
    Records execution of a single task (primitive) during plan execution.
    """
    @spec task_executed(t(), atom(), map(), :success | :running | {:failure, term()}) :: t()
    def task_executed(%__MODULE__{enabled: false} = metrics, _task, _params, _result),
      do: metrics

    def task_executed(%__MODULE__{} = metrics, task_name, params, result) do
      metrics
      |> update_task_count(task_name)
      |> record_decision(:execution_step,
        task: task_name,
        params: params,
        details: %{result: result}
      )
    end

    @doc """
    Records plan execution completion.
    """
    @spec execution_complete(t(), non_neg_integer()) :: t()
    def execution_complete(%__MODULE__{enabled: false} = metrics, _steps), do: metrics

    def execution_complete(%__MODULE__{} = metrics, steps_executed) do
      record_decision(metrics, :execution_complete, details: %{steps_executed: steps_executed})
    end

    @doc """
    Records plan execution failure.
    """
    @spec execution_failed(t(), atom(), term()) :: t()
    def execution_failed(%__MODULE__{enabled: false} = metrics, _task, _reason), do: metrics

    def execution_failed(%__MODULE__{} = metrics, task_name, reason) do
      record_decision(metrics, :execution_failed,
        task: task_name,
        reason: reason
      )
    end

    # --- Query API ---

    @doc """
    Gets the full decision log (newest first).
    """
    @spec get_decision_log(t()) :: [decision_entry()]
    def get_decision_log(%__MODULE__{decisions: decisions}), do: decisions

    @doc """
    Gets decisions filtered by type.
    """
    @spec get_decisions_by_type(t(), decision_type() | [decision_type()]) :: [decision_entry()]
    def get_decisions_by_type(%__MODULE__{decisions: decisions}, types) when is_list(types) do
      Enum.filter(decisions, &(&1.type in types))
    end

    def get_decisions_by_type(metrics, type), do: get_decisions_by_type(metrics, [type])

    @doc """
    Gets decisions for a specific task.
    """
    @spec get_decisions_for_task(t(), atom()) :: [decision_entry()]
    def get_decisions_for_task(%__MODULE__{decisions: decisions}, task_name) do
      Enum.filter(decisions, &(&1.task == task_name))
    end

    @doc """
    Gets a summary of planning statistics.
    """
    @spec get_planning_summary(t()) :: map()
    def get_planning_summary(%__MODULE__{} = metrics) do
      %{
        planning_attempts: metrics.planning_attempts,
        successful_plans: metrics.successful_plans,
        failed_plans: metrics.failed_plans,
        success_rate:
          if(metrics.planning_attempts > 0,
            do: metrics.successful_plans / metrics.planning_attempts * 100,
            else: 0.0
          ),
        total_backtracks: metrics.total_backtrack_count,
        total_iterations: metrics.total_iterations,
        avg_planning_time_us:
          if(metrics.planning_attempts > 0,
            do: div(metrics.planning_time_us, metrics.planning_attempts),
            else: 0
          ),
        method_selection_counts: metrics.method_selection_counts,
        task_execution_counts: metrics.task_execution_counts,
        decisions_logged: metrics.decision_count
      }
    end

    @doc """
    Gets the last N decisions.
    """
    @spec get_recent_decisions(t(), non_neg_integer()) :: [decision_entry()]
    def get_recent_decisions(%__MODULE__{decisions: decisions}, count) do
      Enum.take(decisions, count)
    end

    @doc """
    Formats a decision entry for display.
    """
    @spec format_decision(decision_entry()) :: String.t()
    def format_decision(%{type: type, task: task, method: method, reason: reason} = entry) do
      base =
        case type do
          :task_started ->
            "▶ Planning started for task :#{task}"

          :primitive_success ->
            "✓ Primitive :#{task} added to plan"

          :primitive_failed ->
            "✗ Primitive :#{task} failed: #{inspect(reason)}"

          :compound_decomposed ->
            "◇ Compound :#{task} decomposed"

          :method_selected ->
            "→ Method :#{method} selected for :#{task}"

          :method_rejected ->
            "← Method :#{method} rejected for :#{task}: #{inspect(reason)}"

          :backtrack ->
            "↩ Backtracking from :#{method} on :#{task}: #{inspect(reason)}"

          :plan_found ->
            steps = get_in(entry, [:details, :plan_length]) || 0
            time = get_in(entry, [:details, :planning_time_us]) || 0
            "★ Plan found (#{steps} steps, #{time}μs)"

          :plan_failed ->
            "✗ Planning failed: #{inspect(reason)}"

          :execution_started ->
            steps = get_in(entry, [:details, :total_steps]) || 0
            "▶ Execution started (#{steps} steps)"

          :execution_step ->
            result = get_in(entry, [:details, :result])

            status =
              case result do
                :success -> "✓"
                :running -> "↻"
                _ -> "✗"
              end

            "#{status} Executed :#{task}"

          :execution_complete ->
            steps = get_in(entry, [:details, :steps_executed]) || 0
            "★ Execution complete (#{steps} steps)"

          :execution_failed ->
            "✗ Execution failed at :#{task}: #{inspect(reason)}"

          _ ->
            "? Unknown decision type: #{type}"
        end

      base
    end

    @doc """
    Gets a formatted log suitable for display.
    """
    @spec get_formatted_log(t(), keyword()) :: [String.t()]
    def get_formatted_log(%__MODULE__{} = metrics, opts \\ []) do
      count = Keyword.get(opts, :limit, 50)
      reverse = Keyword.get(opts, :chronological, true)

      decisions =
        metrics
        |> get_recent_decisions(count)
        |> then(fn d -> if reverse, do: Enum.reverse(d), else: d end)

      Enum.map(decisions, &format_decision/1)
    end

    # --- Private Helpers ---

    defp update_method_count(%__MODULE__{} = metrics, method_name) do
      counts = metrics.method_selection_counts
      updated = Map.update(counts, method_name, 1, &(&1 + 1))
      %{metrics | method_selection_counts: updated}
    end

    defp update_task_count(%__MODULE__{} = metrics, task_name) do
      counts = metrics.task_execution_counts
      updated = Map.update(counts, task_name, 1, &(&1 + 1))
      %{metrics | task_execution_counts: updated}
    end
  end
end
