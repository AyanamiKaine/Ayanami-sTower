using FluidHTN.Contexts;

namespace FluidHTN.Conditions
{
    public interface ICondition
    {
        string Name { get; }
        bool IsValid(IContext ctx);
    }
}
