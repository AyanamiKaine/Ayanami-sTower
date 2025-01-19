

namespace jlox;

public class LoxClass(string name, Dictionary<string, LoxFunction> methods) : ILoxCallable
{
    public readonly string Name = name;
    private readonly Dictionary<string, LoxFunction> _methods = methods;

    public int Arity()
    {
        LoxFunction? initalizer = FindMethod("init");
        if (initalizer is null)
            return 0;

        return initalizer.Arity();
    }

    public dynamic? Call(Interpreter interpreter, List<dynamic> arguments)
    {
        LoxInstance instance = new(this);
        LoxFunction? initalizer = FindMethod("init");
        if (initalizer is not null)
            initalizer.Bind(instance).Call(interpreter, arguments);


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