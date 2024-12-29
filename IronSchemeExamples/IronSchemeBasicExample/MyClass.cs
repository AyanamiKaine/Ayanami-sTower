namespace MyNamespace;
public class MyClass
{
    public string Greeting { get; set; } = "Hello from C#!";

    public MyClass()
    {
        Console.WriteLine("MyClass instance created!");
    }

    public string Greet(string name)
    {
        return $"{Greeting} Nice to meet you, {name}!";
    }

    public int Add(int a, int b)
    {
        return a + b;
    }
}