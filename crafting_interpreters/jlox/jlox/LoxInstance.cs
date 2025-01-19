
namespace jlox;

public class LoxInstance(LoxClass klass)
{
    private readonly Dictionary<string, dynamic> fields = [];
    private LoxClass Klass = klass;

    public dynamic Get(Token name)
    {
        if (fields.TryGetValue(name.Lexeme, out dynamic? value))
            return value;

        LoxFunction? method = Klass.FindMethod(name.Lexeme);
        if (method is not null)
            return method.Bind(this);
        throw new RuntimeError(name, $"Undefined property {name.Lexeme}.");
    }

    public override string ToString()
    {
        return Klass.Name + " instance";
    }

    public void Set(Token name, dynamic value)
    {
        if (!fields.ContainsKey(name.Lexeme))
            fields.Add(name.Lexeme, value);
        else
            fields[name.Lexeme] = value;
    }
}