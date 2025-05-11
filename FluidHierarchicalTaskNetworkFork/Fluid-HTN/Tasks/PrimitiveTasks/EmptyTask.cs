using System.Collections.Generic;
using FluidHTN;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Contexts;
using FluidHTN.Operators;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN.PrimitiveTasks
{
    /// <summary>
    /// Represents a task that performs no operation.
    /// This is an implementation of the Null Object pattern for IPrimitiveTask.
    /// </summary>
    public class EmptyTask : IPrimitiveTask
    {
        /// <summary>
        /// Singleton instance of the EmptyTask.
        /// </summary>
        public static readonly EmptyTask Instance = new();

        public string Name { get; } = "EmptyTask";

        public List<ICondition> ExecutingConditions => throw new System.NotImplementedException();

        public IOperator Operator => throw new System.NotImplementedException();

        public List<IEffect> Effects => throw new System.NotImplementedException();

        string ITask.Name
        {
            get => Name;
            set => throw new System.NotImplementedException();
        }
        public ICompoundTask Parent
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public List<ICondition> Conditions => throw new System.NotImplementedException();

        // Private constructor to enforce singleton pattern.
        private EmptyTask() { }

        /// <summary>
        /// Applying effects does nothing for an EmptyTask.
        /// </summary>
        public void ApplyEffects(IContext ctx)
        {
            // No operation
        }

        /// <summary>
        /// An EmptyTask is generally considered valid (it doesn't prevent planning),
        /// but its specific validity might depend on context.
        /// For simplicity, we'll make it always valid.
        /// </summary>
        public bool IsValid(IContext ctx)
        {
            return true; // Or false if an empty task should never be chosen/part of a plan
        }

        public ITask AddExecutingCondition(ICondition condition)
        {
            throw new System.NotImplementedException();
        }

        public void SetOperator(IOperator action)
        {
            throw new System.NotImplementedException();
        }

        public ITask AddEffect(IEffect effect)
        {
            throw new System.NotImplementedException();
        }

        public void Stop(IContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public void Aborted(IContext ctx)
        {
            throw new System.NotImplementedException();
        }

        public ITask AddCondition(ICondition condition)
        {
            throw new System.NotImplementedException();
        }

        public DecompositionStatus OnIsValidFailed(IContext ctx)
        {
            throw new System.NotImplementedException();
        }
    }
}
