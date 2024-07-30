namespace StellaSockets
{
    public class StellaRequestSocket : StellaSocket
    {
        /// <summary>
        ///  In this pattern, a requester sends a message to one replier, who is expected to reply. 
        ///  The request is resent if no reply arrives, until a reply is received or the request times out.
        /// 
        /// This protocol is useful in setting up RPC-like services. 
        /// It is also "reliable", in that a the requester will keep retrying until a reply is received.
        /// </summary>
        /// <param name="address"></param>
        public StellaRequestSocket(string address) : base(SocketType.Request)
        {
            Connect(address);
        }
    }
}