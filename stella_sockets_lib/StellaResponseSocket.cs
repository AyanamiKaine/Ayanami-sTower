namespace StellaSockets
{
    public class StellaResponseSocket : StellaSocket
    {
        /// <summary>
        /// a requester sends a message to one replier, who is expected to reply. 
        /// The request is resent if no reply arrives, until a reply is received or the request times out.
        /// 
        /// This protocol is useful in setting up RPC-like services. 
        /// It is also reliable, in that a requester will keep retrying until a reply is received.
        /// </summary>
        /// <param name="address"></param>
        public StellaResponseSocket(string address) : base(SocketType.Response)
        {
            Bind(address);
        }
    }
}