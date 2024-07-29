namespace StellaSockets
{
    public class StellaRequestSocket : StellaSocket
    {
        public StellaRequestSocket(string address) : base(SocketType.Request)
        {
            Connect(address);
        }
    }
}