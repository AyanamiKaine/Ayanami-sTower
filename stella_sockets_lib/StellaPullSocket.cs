namespace StellaSockets
{
    /// <summary>
    /// Purpose: Creates a one-way data flow from one or more 
    /// senders (push) to one or more receivers (pull).
    /// 
    /// Ideal For: Building data pipelines, processing streams of data, 
    /// or any scenario where you need a linear flow of information.
    /// Use Push and Pull Sockets if you want that many clients
    /// can send messages to a server without expecting a message back
    /// 
    /// Pull is the counterpart to Push.
    /// 
    /// Pushers distribute messages to pullers. 
    /// Each message sent by a pusher will be sent to one of its 
    /// peer pullers, chosen in a round-robin fashion from the 
    /// set of connected peers available for receiving.
    /// 
    /// This property makes this pattern useful in load-balancing 
    /// scenarios.
    /// </summary>
    public class StellaPullSocket : StellaSocket
    {
        /// <summary>
        /// Purpose: Creates a one-way data flow from one or more 
        /// senders (push) to one or more receivers (pull).
        /// 
        /// Ideal For: Building data pipelines, processing streams of data, 
        /// or any scenario where you need a linear flow of information.
        /// Use Push and Pull Sockets if you want that many clients
        /// can send messages to a server without expecting a message back
        /// 
        /// Pull is the counterpart to Push.
        /// 
        /// Pushers distribute messages to pullers. 
        /// Each message sent by a pusher will be sent to one of its 
        /// peer pullers, chosen in a round-robin fashion from the 
        /// set of connected peers available for receiving.
        /// 
        /// This property makes this pattern useful in load-balancing 
        /// scenarios.
        /// </summary>
        public StellaPullSocket(string address) : base(SocketType.Pull)
        {
            Bind(address);
        }
    }
}