using Metalama.Patterns.Contracts;

namespace HelloWorldExample;

/// <summary>
/// Showing the usage of contracts
/// </summary>
[PrettyPrint]
public class Customer
{

    /// <summary>
    /// required name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Here we define a contract that says the BirthYear can only be
    /// between 1900 and 2100
    /// </summary>
    [Range(1900, 2100)]
    public int BirthYear { get; set; }
}

/// <summary>
/// Program
/// </summary>
public static class Program
{
    /// <summary>
    /// main entry
    /// </summary>
    public static void Main()
    {
        try
        {
            var tom = new Customer()
            {
                Name = "Tom",
                BirthYear = 200 // WILL THROW
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        try
        {
            var tom = new Customer()
            {
                Name = "Tom",
                BirthYear = 2000 // WONT THROW
            };

            Console.WriteLine(tom);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}