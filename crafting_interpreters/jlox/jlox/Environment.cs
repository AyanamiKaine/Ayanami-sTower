using Microsoft.VisualBasic;

namespace jlox;

public class LoxEnvironment
{
    private readonly Dictionary<string, dynamic> values = [];
    private readonly LoxEnvironment? _enclosing = null;

    public LoxEnvironment()
    {

    }

    public LoxEnvironment(LoxEnvironment enclosing)
    {
        _enclosing = enclosing;
    }

    public void Define(string name, dynamic value)
    {
        values.Add(name, value);
    }

    public dynamic Get(Token name)
    {
        if (values.TryGetValue(name.Lexeme, out var value))
            return value;

        // If the variable isn’t found in this environment, 
        // we simply try the enclosing one recursivly
        if (_enclosing is not null)
            return _enclosing.Get(name);
        else
            throw new RuntimeError(name, $"Undefined Variable {name.Lexeme}.");
    }

    public void Assign(Token name, dynamic value)
    {
        if (values.ContainsKey(name.Lexeme))
        {
            values[name.Lexeme] = value;
            return;
        }
        if (_enclosing is not null)
        {
            _enclosing.Assign(name, value);
            return;
        }
        else
            throw new RuntimeError(name, $"Undefined Variable {name.Lexeme}.");
    }
}