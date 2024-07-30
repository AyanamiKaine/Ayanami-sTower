using Stella.Testing;

namespace StellaSockets
{
    public class StellaSocketsUnitTests
    {
        [ST_TEST]
        public static StellaTesting.TestingResult PairSocketTest()
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
        public static StellaTesting.TestingResult RequestRespondSocketMessageTest()
        {
            using var Server = new StellaResponseSocket("ipc:///hello_world_ReqRes_unit_test");
            using var Client = new StellaRequestSocket("ipc:///hello_world_ReqRes_unit_test");

            Client.Send("Hello World");
            string actualMessage = Server.Receive();
            Server.Send("Message Received");
            Client.Receive();

            string expectedMessage = "Hello World";

            return StellaTesting.AssertEqual(expectedMessage, actualMessage);
        }


        [ST_TEST]
        public static StellaTesting.TestingResult PullPushSocketMessageTest()
        {

            using var Server = new StellaPullSocket("ipc:///pull_push_unit_test");

            using var ClientA = new StellaPushSocket("ipc:///pull_push_unit_test");
            ClientA.Send("Hello World from Client A");

            using var ClientB = new StellaPushSocket("ipc:///pull_push_unit_test");
            ClientB.Send("Hello World from Client B");

            string messageA = Server.Receive();
            string messageB = Server.Receive();

            return StellaTesting.AssertTrue(messageA == "Hello World from Client A" && messageB == "Hello World from Client B", "Expected Messages were incorret!");
        }

        /*
                [ST_TEST]
                public static StellaTesting.TestingResult BusSocketMessageTest()
                {
                    /*
                    using var ServerA = new StellaBusSocket();
                    ServerA.Bind("ipc:///hello_world_bus_unit_test");

                    using var ClientA = new StellaBusSocket();
                    ClientA.Connect("ipc:///hello_world_bus_unit_test");
                    ClientA.Send("Hello World");

                    using var ClientB = new StellaBusSocket();
                    ClientB.Connect("ipc:///hello_world_bus_unit_test");
                    ClientB.Send("Hello World");

                    string expectedMessage = "Hello World";
                    string actualMessage = ServerA.Receive();

                    return StellaTesting.AssertEqual(expectedMessage, "actualMessage");
                    
        }
    */
    }
}