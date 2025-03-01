

namespace jlox;

public class LoxFunction(Statement.Function declaration, LoxEnvironment closure, bool isInitalizer) : ILoxCallable
{
    private readonly Statement.Function _declaration = declaration;
    private readonly LoxEnvironment _closure = closure;

    private readonly bool _isInitalizer = isInitalizer;
    public int Arity()
    {
        return _declaration.params_.Count;
    }

    public dynamic? Call(Interpreter interpreter, List<dynamic> arguments)
    {
        LoxEnvironment environment = new(_closure);
        for (int i = 0; i < _declaration.params_.Count; i++)
        {
            environment.Define(_declaration.params_[i].Lexeme, arguments[i]);
        }
        try
        {
            interpreter.ExecuteBlock(_declaration.body, environment);
        }
        catch (Return returnValue)
        {
            if (_isInitalizer)
                return _closure.GetAt(0, "this");

            return returnValue.Value;
        }
        return null;
    }

    public override string ToString()
    {
        return $"<fn {_declaration.name.Lexeme}>";
    }

    public LoxFunction Bind(LoxInstance instance)
    {
        LoxEnvironment environment = new(_closure);
        environment.Define("this", instance);
        return new LoxFunction(_declaration, environment, _isInitalizer);
    }
}