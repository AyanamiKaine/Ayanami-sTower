using MessagePack;

namespace Mens;

[MessagePackObject]
public class TestMessage(string message)
{
    [Key(0)]
    public string Message = message;
}


public class TestState
{

}

public class Anima : Vita<TestState, TestMessage>
{

}
