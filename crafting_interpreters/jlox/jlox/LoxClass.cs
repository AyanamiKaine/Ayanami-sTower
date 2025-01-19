
namespace jlox;

public class LoxClass(string name) : ILoxCallable
{
    public readonly string Name = name;

    public int Arity()
    {
        return 0;
    }

    public dynamic? Call(Interpreter interpreter, List<dynamic> arguments)
    {
        LoxInstance instance = new(this);
        return instance;
    }

    public override string ToString()
    {
        return Name;
    }
}