namespace StellaSockets
{



    /// <summary>
    /// The subscribing applications only see the data to which they have subscribed.
    /// </summary>
    public class StellaSubscriberSocket : StellaSocket
    {
        public StellaSubscriberSocket(string address) : base(SocketType.Sub)
        {
            Connect(address);
        }

        public void Subscribe(string topic)
        {
            StellaMessagingInterop.subscribed_to_topic(_socketHandle, topic);

        }
        public void Unsubscribe(string topic)
        {
            StellaMessagingInterop.unsubscribed_to_topic(_socketHandle, topic);
        }
    }
}