﻿using System;
using System.Collections.Generic;
using FluidHTN.Conditions;
using FluidHTN.Contexts;
using FluidHTN.PrimitiveTasks;

namespace FluidHTN.Compounds
{
    public abstract class CompoundTask : ICompoundTask
    {
        // ========================================================= PROPERTIES

        public string Name { get; set; }
        public ICompoundTask Parent { get; set; }
        public List<ICondition> Conditions { get; } = [];
        public List<ITask> Subtasks { get; } = [];

        // ========================================================= VALIDITY

        public virtual DecompositionStatus OnIsValidFailed(IContext ctx)
        {
            return DecompositionStatus.Failed;
        }

        // ========================================================= ADDERS

        public ITask AddCondition(ICondition condition)
        {
            Conditions.Add(condition);
            return this;
        }

        public ICompoundTask AddSubtask(ITask subtask)
        {
            Subtasks.Add(subtask);
            return this;
        }

        // ========================================================= DECOMPOSITION

        public DecompositionStatus Decompose(IContext ctx, int startIndex, out Queue<ITask> result)
        {
            if (ctx.LogDecomposition)
            {
                ctx.CurrentDecompositionDepth++;
            }

            var status = OnDecompose(ctx, startIndex, out result);

            if (ctx.LogDecomposition)
            {
                ctx.CurrentDecompositionDepth--;
            }

            return status;
        }

        protected abstract DecompositionStatus OnDecompose(
            IContext ctx,
            int startIndex,
            out Queue<ITask> result
        );

        protected abstract DecompositionStatus OnDecomposeTask(
            IContext ctx,
            ITask task,
            int taskIndex,
            int[] oldStackDepth,
            out Queue<ITask> result
        );

        protected abstract void OnDecomposePrimitiveTask(
            IContext ctx,
            IPrimitiveTask task,
            int taskIndex,
            int[] oldStackDepth,
            out Queue<ITask> result
        );

        protected abstract DecompositionStatus OnDecomposeCompoundTask(
            IContext ctx,
            ICompoundTask task,
            int taskIndex,
            int[] oldStackDepth,
            out Queue<ITask> result
        );

        protected abstract DecompositionStatus OnDecomposeSlot(
            IContext ctx,
            Slot task,
            int taskIndex,
            int[] oldStackDepth,
            out Queue<ITask> result
        );

        // ========================================================= VALIDITY

        public virtual bool IsValid(IContext ctx)
        {
            foreach (var condition in Conditions)
            {
                var result = condition.IsValid(ctx);

                if (ctx.LogDecomposition)
                {
                    Log(
                        ctx,
                        $"CompoundTask.IsValid:{(result ? "Success" : "Failed")}:{condition.Name} is{(result ? "" : " not")} valid!",
                        result ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed
                    );
                }

                if (!result)
                {
                    return false;
                }
            }

            return true;
        }

        // ========================================================= LOGGING

        protected virtual void Log(
            IContext ctx,
            string description,
            ConsoleColor color = ConsoleColor.White
        )
        {
            ctx.Log(Name, description, ctx.CurrentDecompositionDepth, this, color);
        }
    }
}
