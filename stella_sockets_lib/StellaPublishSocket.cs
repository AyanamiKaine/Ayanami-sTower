namespace StellaSockets
{
    /// <summary>
    /// This socket may be used to send messages, but is unable to receive them. 
    /// THIS IS NOT YET IMPLEMENTED
    /// </summary>
    internal class StellaPublishSocket : StellaSocket
    {
        internal StellaPublishSocket() : base(SocketType.Pub)
        {
            throw new NotImplementedException();
        }
    }
}