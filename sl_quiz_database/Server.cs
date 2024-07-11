using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace sl_quiz_database
{
    public class Server
    {

        public ResponseSocket QuizDatabase = new("@tcp://localhost:60020");

        public void Run()
        {
            Console.WriteLine("Starting Quiz Database");

            while (true)
            {
                string json_request = QuizDatabase.ReceiveFrameString();
                Request request = JsonSerializer.Deserialize<Request>(json_request);
            
                switch (request.Command)
                {
                    case Command.Create:
                        break;

                    case Command.Update:
                        break;

                    case Command.Delete:
                        break;

                    case Command.Retrieve:
                        break;


                    default:
                        Console.WriteLine("Invalid request");
                        break;
                }
            
            }
        }
    }
}
