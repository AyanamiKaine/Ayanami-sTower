namespace jlox;

public interface LoxCallable
{
    int Arity();
    dynamic Call(Interpreter interpreter, List<dynamic> arguments);
}