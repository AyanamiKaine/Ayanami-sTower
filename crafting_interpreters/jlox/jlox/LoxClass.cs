namespace jlox;

public class LoxClass(string name)
{
    public readonly string Name = name;
    public override string ToString()
    {
        return Name;
    }
}