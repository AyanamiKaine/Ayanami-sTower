namespace StellaSockets
{
    /// <summary>
    /// Purpose: Creates a many-to-many communication channel 
    /// where all connected peers can send and receive messages.
    /// 
    /// Ideal For: Building decentralized systems, peer-to-peer networks, 
    /// or any scenario where you need a flexible communication model.
    /// </summary>
    public class StellaBusSocket : StellaSocket
    {
        /// <summary>
        /// Purpose: Creates a many-to-many communication channel 
        /// where all connected peers can send and receive messages.
        /// 
        /// Ideal For: Building decentralized systems, peer-to-peer networks, 
        /// or any scenario where you need a flexible communication model.
        /// 
        /// NOT IMPLEMENTED!
        /// </summary>
        public StellaBusSocket() : base(SocketType.Bus)
        {
            throw new NotImplementedException();
        }
    }
}