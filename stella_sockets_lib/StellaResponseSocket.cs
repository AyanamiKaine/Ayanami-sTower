namespace StellaSockets
{
    public class StellaResponseSocket : StellaSocket
    {
        public StellaResponseSocket(string address) : base(SocketType.Response)
        {
            Bind(address);
        }
    }
}