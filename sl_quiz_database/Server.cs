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

        public List<Question> Questions = [];

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
        public void CreateQuestion(string questionText)
        {
            Question question = new(questionText);
            Questions.Add(question);
        }

        public List<Question> RetrieveQuestion()
        {
            if (Questions.Count == 0)
            {
                return null; // Or throw an exception
            }
            // For simplicity, always return the first question
            return Questions; 
        }

        public void DeleteQuestion(string questionText)
        {
            Question questionToRemove = Questions.FirstOrDefault(q => q.QuestionText == questionText);
            if (questionToRemove != null)
            {
                Questions.Remove(questionToRemove);
            }
            else
            {
                Console.WriteLine($"Question not found: {questionText}");
            }
        }

        public void UpdateQuestion(string questionData)
        {
            // Deserialize the updated question data (JSON)
            Question updatedQuestion = JsonSerializer.Deserialize<Question>(questionData);

            // Find the question to update (using ID, text, or other identifier)
            Question questionToUpdate = Questions.FirstOrDefault(q => q.QuestionText == updatedQuestion.QuestionText);

            if (questionToUpdate != null)
            {
                // Update the properties of the existing question
                questionToUpdate.AnswerOptions = updatedQuestion.AnswerOptions;
                questionToUpdate.CorrectAnswer = updatedQuestion.CorrectAnswer;
            }
            else
            {
                Console.WriteLine($"Question not found: {updatedQuestion.QuestionText}");
            }
        }
    }
}
