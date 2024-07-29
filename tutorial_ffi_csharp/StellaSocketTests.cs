using Stella.Testing;

namespace t_ffi
{
    public class StellaSocketTests
    {

        [ST_TEST]
        private static StellaTesting.TestingResult PairSocketTest()
        {

            using var Server = new StellaSocket(SocketType.Pair);
            Server.Bind("ipc:///hello_world_unit_test");

            using var Client = new StellaSocket(SocketType.Pair);
            Client.Connect("ipc:///hello_world_unit_test");
            Client.Send("Hello World");

            string message = Server.Receive();

            return StellaTesting.AssertEqual("Hello World", message);
        }


        [ST_TEST]
        private static StellaTesting.TestingResult PullPushSocketMessageTest()
        {

            using var Server = new StellaSocket(SocketType.Pull);
            Server.Bind("ipc:///pull_push_unit_test");

            using var ClientA = new StellaSocket(SocketType.Push);
            ClientA.Connect("ipc:///pull_push_unit_test");
            ClientA.Send("Hello World from Client A");

            using var ClientB = new StellaSocket(SocketType.Push);
            ClientB.Connect("ipc:///pull_push_unit_test");
            ClientB.Send("Hello World from Client B");

            string messageA = Server.Receive();
            string messageB = Server.Receive();

            return StellaTesting.AssertTrue(messageA == "Hello World from Client A" && messageB == "Hello World from Client B", "Expected Messages were incorret!");
        }

    }
}