namespace jlox;

public class LoxInstance(LoxClass klass)
{
    private LoxClass Klass = klass;

    public override string ToString()
    {
        return Klass.Name + " instance";
    }
}