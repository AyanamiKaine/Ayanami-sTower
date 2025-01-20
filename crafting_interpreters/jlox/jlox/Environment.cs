using Microsoft.VisualBasic;

namespace jlox;

public class LoxEnvironment
{
    public readonly Dictionary<string, dynamic> Values = [];
    public readonly LoxEnvironment? Enclosing = null;

    public LoxEnvironment()
    {

    }

    public LoxEnvironment(LoxEnvironment enclosing)
    {
        Enclosing = enclosing;
    }

    public void Define(string name, dynamic value)
    {
        Values.Add(name, value);
    }

    public dynamic Get(Token name)
    {
        if (Values.TryGetValue(name.Lexeme, out var value))
            return value;

        // If the variable isn’t found in this environment, 
        // we simply try the enclosing one recursivly
        if (Enclosing is not null)
            return Enclosing.Get(name);
        else
            throw new RuntimeError(name, $"Undefined Variable {name.Lexeme}.");
    }

    public void Assign(Token name, dynamic value)
    {
        if (Values.ContainsKey(name.Lexeme))
        {
            Values[name.Lexeme] = value;
            return;
        }
        if (Enclosing is not null)
        {
            Enclosing.Assign(name, value);
            return;
        }
        else
            throw new RuntimeError(name, $"Undefined Variable {name.Lexeme}.");
    }

    public dynamic GetAt(int distance, string name)
    {
        return Ancestor(distance).Values[name];
    }

    private LoxEnvironment Ancestor(int distance)
    {
        var env = this;
        for (int i = 0; i < distance; i++)
        {
            env = env.Enclosing;
        }

        return env;
    }

    public void AssignAt(int distance, Token name, dynamic value)
    {
        var values = Ancestor(distance).Values;
        if (!values.ContainsKey(name.Lexeme))
            values.Add(name.Lexeme, value);
        else
            values[name.Lexeme] = value;
    }
}