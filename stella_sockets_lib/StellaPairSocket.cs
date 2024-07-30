namespace StellaSockets
{
    /// <summary>
    /// Purpose: Establishes a one-to-one connection between two peers.
    /// 
    /// Beware: There can only be one pair, you cannot connect a third pair socket to a socket pair that already
    /// is connect to each other.
    /// 
    /// Ideal For: Simple, direct communication where 
    /// you need a reliable channel between two endpoints.
    /// 
    /// If you use the generic socket you must call bind() and connect() yourself
    /// </summary>
    public class StellaPairGenericSocket : StellaSocket
    {
        public StellaPairGenericSocket() : base(SocketType.Pair)
        {
            // Do Pair sockets need to always bind and connect at the same time?
            // Or is it enough when one does it?
        }
    }

    public class StellaPairClientSocket : StellaPairGenericSocket
    {
        public StellaPairClientSocket(string address) : base()
        {
            Connect(address);
        }
    }

    public class StellaPairServerSocket : StellaPairGenericSocket
    {
        public StellaPairServerSocket(string address) : base()
        {
            Bind(address);
        }
    }
}