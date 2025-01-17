using Microsoft.VisualBasic;

namespace jlox;

public class LoxEnvironment
{
    private readonly Dictionary<string, dynamic> values = [];


    public void Define(string name, dynamic value)
    {
        values.Add(name, value);
    }

    public dynamic Get(Token name)
    {
        if (values.TryGetValue(name.Lexeme, out var value))
            return value;
        else
            throw new RuntimeError(name, $"Undefined Variable {name.Lexeme}.");
    }
}