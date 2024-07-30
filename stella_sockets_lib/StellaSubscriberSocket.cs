namespace StellaSockets
{



    /// <summary>
    /// The subscribing applications only see the data to which they have subscribed.
    /// THIS IS NOT YET IMPLEMENTED
    /// </summary>
    internal class StellaSubscriberSocket : StellaSocket
    {
        internal StellaSubscriberSocket() : base(SocketType.Pub)
        {
            throw new NotImplementedException();
        }
    }
}