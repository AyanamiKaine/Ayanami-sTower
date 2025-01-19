namespace jlox;

public interface ILoxCallable
{
    int Arity();
    dynamic Call(Interpreter interpreter, List<dynamic> arguments);
}