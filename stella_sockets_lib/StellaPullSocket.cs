namespace StellaSockets
{
    /// <summary>
    /// Purpose: Creates a one-way data flow from one or more 
    /// senders (push) to one or more receivers (pull).
    /// 
    /// Ideal For: Building data pipelines, processing streams of data, 
    /// or any scenario where you need a linear flow of information.
    /// </summary>
    public class StellaPushSocket : StellaSocket
    {
        public StellaPushSocket(string address) : base(SocketType.Push)
        {
            Connect(address);
        }
    }
}