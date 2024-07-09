using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace SpaceRepetitionAlgorithm
{
    internal class Server
    {
        public ResponseSocket server = new("@tcp://localhost:60005");
        public DealerSocket LoggerClient = new DealerSocket(">tcp://localhost:60010");    // Clients always start with an >

        public void Run()
        {
            LogMessage("Starting Spaced Repetition Algorithm");

            try
            {

                while (true)
                {
                    string json_request = server.ReceiveFrameString();

                    Request request = JsonSerializer.Deserialize<Request>(json_request);


                    DateTime new_due_date = SpacedRepetitionScheduler.CalculateNextReviewDate(request.FileToLearn, request.RecallEvaluation);
                    LogMessage($"New calculated due date: {new_due_date.ToString()}, for, {request.FileToLearn.Name}");

                    string json_response = JsonSerializer.Serialize(new_due_date);

                    server.SendFrame($"{json_response}");
                }
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message);
            }
        }

        private void LogMessage(string message)
        {
            LoggerClient.SendFrame($"{{ \"Message\": \"{message}\" }}");
        }
    }
}
