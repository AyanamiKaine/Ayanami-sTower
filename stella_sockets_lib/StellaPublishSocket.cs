namespace StellaSockets
{
    /// <summary>
    /// This socket may be used to send messages, but is unable to receive them. 
    /// THIS IS NOT YET IMPLEMENTED
    /// </summary>
    public class StellaPublishSocket : StellaSocket
    {
        public StellaPublishSocket(string address) : base(SocketType.Pub)
        {
            Bind(address);
        }
    }
}