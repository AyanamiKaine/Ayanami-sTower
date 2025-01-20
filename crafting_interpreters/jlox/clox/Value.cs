namespace clox;

/// <summary>
/// We start simple and for now a lox value can only be a double
/// </summary>
/// <param name="value"></param>
public struct LoxValue(double value)
{
    public double Value = value;
}