using SlLogger;

namespace Logger
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Server server = new Server();
            //server.Run();

                StellaLogger Server = new();
                Server.Run();

        }
    }
}
