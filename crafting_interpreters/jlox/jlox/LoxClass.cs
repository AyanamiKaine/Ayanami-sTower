

namespace jlox;

public class LoxClass(string name, LoxClass? superClass, Dictionary<string, LoxFunction> methods) : ILoxCallable
{
    public readonly string Name = name;
    public readonly LoxClass? SuperClass = superClass;
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
        initalizer?.Bind(instance).Call(interpreter, arguments);


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

        // If we want to call a method that is defined in the super class
        // we call that. If the method is already defined in the base class
        // we call that instead. 
        if (SuperClass is not null)
            return SuperClass.FindMethod(name);

        return null;
    }
}