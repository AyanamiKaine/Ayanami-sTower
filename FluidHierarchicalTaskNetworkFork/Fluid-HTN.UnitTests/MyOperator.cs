using FluidHTN;
using FluidHTN.Contexts;
using FluidHTN.Operators;

namespace Fluid_HTN.UnitTests
{
    internal class MyOperator : IOperator
    {
        public FluidHTN.TaskStatus Update(IContext ctx)
        {
            return FluidHTN.TaskStatus.Continue;
        }

        public void Stop(IContext ctx) { }

        public void Aborted(IContext ctx) { }
    }
}
