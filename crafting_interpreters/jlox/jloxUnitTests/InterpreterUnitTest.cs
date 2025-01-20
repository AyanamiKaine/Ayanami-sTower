using jlox;
namespace jloxUnitTests;

public class InterpreterUnitTest
{
    [Fact]
    public void SimpleMathExpression()
    {
        var expression = """( 1 + 2 ) * 3""";
        var scanner = new Scanner(expression);

        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);

        Expr? expr = parser.ParseAndReturnExpression();
        Assert.NotNull(expr);


        var interpreter = new Interpreter();

        var expectedResult = 9d;
        var actualResult = interpreter.Interpret(expr);
        Assert.NotNull(actualResult);

        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public void ClassInheritanceTest()
    {
        var source =
        """
        class Doughnut { } 
        
        class BostomCream < Doughnut {}
        """;
        var errorHappend = false;
        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void ClassCantInheritFromItSelfTest()
    {
        var source =
        """
        class Oops < Oops { }         
        """;
        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.True(errorHappend);
    }

    [Fact]
    public void ClassInheritanceSuperClassCall()
    {
        var source =
        """
        class Doughnut 
        { 
            cook() 
            { 
                print "Fry until golden brown."; 
            } 
        
        } 
        class BostonCream < Doughnut {} 
        BostonCream().cook();
        """;

        var errorHappend = false;
        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void ClassCallBackTest()
    {
        var source =
        """
        class Thing 
        { 
            getCallback() 
            { 
                fun localFunction() 
                { 
                    print this; 
                } 
                
                return localFunction; 
            } 
        } 
        
        var callback = Thing().getCallback();
        callback();
        """;
        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void ClassThisMethodCallTest()
    {
        var source =
        """
        class Cake 
        { 
            taste() 
            { 
                var adjective = "delicious"; 
                print "The " + this.flavor + " cake is " + adjective + "!"; 
            } 
        } 
            
        var cake = Cake(); 
        cake.flavor = "German chocolate"; 
        cake.taste();
        """;

        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void ClassMethodCallTest()
    {
        var source =
        """
        class Beacon 
        { 
            eat()
            {
                print "Crunch crunch crunch!"; 
            } 
        }
        Beacon().eat();     
        """;

        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void PrintClassInstanceName()
    {
        var source =
        """
        class Bagel {} 
        var bagel = Bagel(); 
        print bagel;
        """;

        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void SimpleBooleanExpressionTest()
    {
        var expression = """(2 == 2) == true""";
        var scanner = new Scanner(expression);

        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);

        Expr? expr = parser.ParseAndReturnExpression();

        // If the expr is null the parse had an error
        Assert.NotNull(expr);

        var interpreter = new Interpreter();

        var expectedResult = true;
        var actualResult = interpreter.Interpret(expr);
        Assert.NotNull(actualResult);

        Assert.Equal(expectedResult, actualResult);
    }

    [Fact]
    public void SimpleAssignment()
    {
        var expression = """var name = "Ayanami"; """;
        var scanner = new Scanner(expression);

        var tokens = scanner.ScanTokens();
        var parser = new Parser(tokens);

        var statements = parser.Parse();


        // If statements is empty we couldnt parse it correctly
        Assert.NotEmpty(statements);
        foreach (var statement in statements)
        {
            Assert.NotNull(statement);
        }

        // Simply, when an error happens in the interpreter
        // it throws
        var interpreter = new Interpreter();
        interpreter.Interpret(statements);
    }

    [Fact]
    public void FibonacciExample()
    {
        var source = """
        fun fib(n) 
        { 
            if (n <= 1) return n; 
            return fib(n - 2) + fib(n - 1); 
        } 
        
        for (var i = 0; i < 20; i = i + 1) { 
            print fib(i); 
        }
        """;
        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void ReassignmentExample()
    {
        var source =
        """
                var a = 0; 
        var temp; 
        
        for (var b = 1; a < 10000; b = temp + b) 
        { 
            print a; 
            temp = a; 
            a = b; 
        }
        """;
        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void FunctionCallMultipleArguments()
    {
        var source =
        """
                fun sayHi(first, last) 
        { 
            print "Hi, " + first + " " + last + "!"; 
        } 
        
        sayHi("Dear", "Reader");
        """;
        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void FunctionCallPrint()
    {
        var source =
        """
        fun add(a, b) { print a + b; }
        """;
        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }

    [Fact]
    public void NativeFunctionCall()
    {
        //Here the clock function is a native defined function that is in the global scope
        var source =
        """
        clock();
        """;
        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }


    /// <summary>
    /// You cannot refrence the same variable name in the outer scope
    /// </summary>
    [Fact]
    public void RefrenceVariableInInitalizerError()
    {
        var source =
        """
        var test = "Outer";
        {
            var test = test;
        }
        """;

        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.True(errorHappend);
    }

    /// <summary>
    /// Here we should be able to refrence the outer variable as it has 
    /// a different name.
    /// </summary>
    [Fact]
    public void RefrenceVariableInInitalizerNOError()
    {
        var source =
        """
        var outer = "Outer";
        {
            var innter = outer;
        }
        """;

        var errorHappend = false;

        var lox = new Lox();
        try
        {
            lox.Run(source);
        }
        catch (Exception)
        {
            errorHappend = true;
        }

        Assert.False(errorHappend);
    }
}