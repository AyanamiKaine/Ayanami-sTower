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
    /// </summary>
    public class StellaPairSocket : StellaSocket
    {
        public StellaPairSocket() : base(SocketType.Pair)
        {
            // Do Pair sockets need to always bind and connect at the same time?
            // Or is it enough when one does it?
        }
    }
}