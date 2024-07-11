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

        public ResponseSocket QuizDatabaseServer = new("@tcp://localhost:60020");
        private readonly QuizDatabase _quizDatabase = new();
        public void Run()
        {
            Console.WriteLine("Starting Quiz Database");

            while (true)
            {
                string json_request = QuizDatabaseServer.ReceiveFrameString();
                Request request = JsonSerializer.Deserialize<Request>(json_request);
            
                switch (request.Command)
                {
                    case Command.Create:
                        _quizDatabase.CreateQuestion(request.question.QuestionText);
                        break;

                    case Command.Update:
                        _quizDatabase.UpdateQuestion(request.question);
                        break;

                    case Command.Delete:
                        _quizDatabase.DeleteQuestion(request.question);
                        break;

                    case Command.Retrieve:
                        QuizDatabaseServer.SendFrame(_quizDatabase.RetrieveQuestionsAsJson());
                        break;

                    default:
                        Console.WriteLine("Invalid request");
                        break;
                }
            
            }
        }
    }
}
