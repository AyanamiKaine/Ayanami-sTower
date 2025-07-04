﻿using FluidHTN.Contexts;

namespace FluidHTN.Operators
{
    public interface IOperator
    {
        TaskStatus Update(IContext ctx);
        void Stop(IContext ctx);
        void Aborted(IContext ctx);
    }
}
