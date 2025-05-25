using System;
using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;
namespace ErrorHandling;

/// <summary>
/// Here we explore the different ways we can throw erorrs. This does not go in detail how to handle them
/// </summary>
public class ThrowingErrorUnitTest
{

    private struct Error()
    {
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// In modern C# functions can easily returns multiple objects without using the out modifier.
    /// Tuples are mutable value containers. They are value tuples for more see: "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples"
    /// 
    /// 
    /// Here we are not using exceptions but we are using null and need to check if an error is null to either
    /// handle the error or to proceed.
    /// </summary>
    [Fact]
    public void UsingTuples()
    {
        static (Error?, int) AddEvenNumbers(int x, int y)
        {
            if (x % 2 != 0 || y % 2 != 0)
                return (new Error() { Message = "Numbers not even!" }, 0);

            return (null, x + y);
        }

        (var error, var value) = AddEvenNumbers(10, 20);

        if (error is null)
        {
            // Handle the error here. 
            // abort if needed
        }

        // program procedes...

        Assert.Equal(30, value);
        Assert.Null(error);
    }

    /// <summary>
    /// Option types are a new dotnet 10 feature (previsouly a dotnet 9 feature that got pushed back) that are quite similar to Rusts Result type. Using dicriminated unions. Currently if you want to use the feature in dotnet 9 you
    /// have to use LanguageExt.Core or https://github.com/altmann/FluentResults or https://github.com/vkhorikov/CSharpFunctionalExtensions or https://github.com/mcintyre321/OneOf or https://github.com/domn1995/dunet or your own implementation or any other implementation. The idea is always the same.
    /// On the side note it seems like there exists really many different implementations. When dotnet 10 rolls around we should use that. Also see https://github.com/louthy/language-ext/wiki for excellent documentation
    /// </summary>
    [Fact]
    public void UsingResultTypesSuccess()
    {
        // In LanguageExt.Core fin is annalogous to Result<R, E> found in rust.
        // While in the current nuget 4.6 a result type exist it gets removed in v5
        // because people used it where Fin was more appropriate.
        static Fin<int> AddEvenNumbers(int x, int y)
        {
            if (x % 2 != 0 || y % 2 != 0)
                return Fin<int>.Fail(new ArgumentException($"Numbers are not even! x:{x} y:{y}"));

            return x + y;
        }

        var result = AddEvenNumbers(10, 20);
        Assert.True(result.IsSucc);
    }

    /// <summary>
    /// Result type returning error
    /// </summary>
    [Fact]
    public void UsingResultTypesFailure()
    {
        // In LanguageExt.Core fin is annalogous to Result<R, E> found in rust.
        // While in the current nuget 4.6 a result type exist it gets removed in v5
        // because people used it where Fin was more appropriate.
        static Fin<int> AddEvenNumbers(int x, int y)
        {
            // We dont have to create a new exception we could just return a error message.
            // But what is the best practice here?
            // To better understand when to do what we need to get a better grasp 
            // To crash and die or to recover that is the question we need to ask.
            // See the error handling note for more information. 
            if (x % 2 != 0 || y % 2 != 0)
                return Fin<int>.Fail(new($"Numbers are not even! x:{x} y:{y}"));

            return x + y;
        }

        var result = AddEvenNumbers(10, 25);
        Assert.True(result.IsFail);
    }
}
