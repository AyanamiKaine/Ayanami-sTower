

namespace jlox;

public class LoxClass(string name, Dictionary<string, LoxFunction> methods) : ILoxCallable
{
    public readonly string Name = name;
    private readonly Dictionary<string, LoxFunction> _methods = methods;

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

    public LoxFunction? FindMethod(string name)
    {
        if (_methods.TryGetValue(name, out LoxFunction? value))
            return value;

        return null;
    }
}