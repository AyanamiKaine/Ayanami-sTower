using LanguageExt;
using LanguageExt.Common;
namespace ErrorHandling;
#pragma warning disable CS1591 // Add summary to documentation comment

/// <summary>
/// Here we explore the different ways we can handle erorrs. This does not go in detail how to throw them
/// </summary>
public class HandleErrorUnitTest
{

    public enum NumberErrorCodes
    {
        NotEven = 0
    }


    static Fin<int> AddEvenNumbers(int x, int y)
    {
        if (x % 2 != 0 || y % 2 != 0)
            return Fin<int>.Fail(Error.New((int)NumberErrorCodes.NotEven, $"Numbers are not even! x:{x} y:{y}"));

        return x + y;
    }

    static int HandleError(Error err)
    {
        Console.WriteLine(err.Message);

        if (err.Code == (int)NumberErrorCodes.NotEven)
            throw new Exception("Couldnt recover");

        // Returns a default value
        return 0;
    }

    [Fact]
    public void UsingResultTypesSuccess()
    {
        var result = AddEvenNumbers(10, 20);
        result = result.IfFail(HandleError);
        result.IfSucc((value) => Assert.Equal(30, value));
    }
}
