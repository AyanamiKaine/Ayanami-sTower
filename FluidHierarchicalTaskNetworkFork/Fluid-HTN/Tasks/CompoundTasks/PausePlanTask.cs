﻿using System;
using System.Collections.Generic;
using FluidHTN.Compounds;
using FluidHTN.Conditions;
using FluidHTN.Contexts;

namespace FluidHTN
{
    public class PausePlanTask : ITask
    {
        // ========================================================= PROPERTIES

        public string Name { get; set; }
        public ICompoundTask Parent { get; set; }
        public List<ICondition> Conditions { get; } = null;
        public List<IEffect> Effects { get; } = null;

        // ========================================================= VALIDITY

        public DecompositionStatus OnIsValidFailed(IContext ctx)
        {
            return DecompositionStatus.Failed;
        }

        // ========================================================= ADDERS

        public ITask AddCondition(ICondition condition)
        {
            throw new Exception("Pause Plan tasks does not support conditions.");
        }

        public static ITask AddEffect(IEffect effect)
        {
            throw new Exception("Pause Plan tasks does not support effects.");
        }

        // ========================================================= FUNCTIONALITY

        public static void ApplyEffects(IContext ctx) { }

        // ========================================================= VALIDITY

        public bool IsValid(IContext ctx)
        {
            if (ctx.LogDecomposition)
            {
                Log(ctx, $"PausePlanTask.IsValid:Success!");
            }

            return true;
        }

        // ========================================================= LOGGING

        protected virtual void Log(IContext ctx, string description)
        {
            ctx.Log(Name, description, ctx.CurrentDecompositionDepth, this, ConsoleColor.Green);
        }
    }
}
