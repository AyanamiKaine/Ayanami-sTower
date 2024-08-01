using StellaSockets;

namespace SpaceRepetitionAlgorithm
{
    public class StellaSRA
    {
        private readonly StellaResponseSocket _server;

        public StellaSRA()
        {
            _server = new StellaResponseSocket("ipc:///StellaSRA");
        }

        public void Run()
        {
            while (true)
            {
                string request = _server.Receive();

                HandleMessage(request);
            }
        }

        private void HandleMessage(string message)
        {

        }
    }
}