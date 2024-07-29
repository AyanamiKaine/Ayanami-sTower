using System.Text.Json;

namespace sl_quiz_database
{
    public class QuizDatabase
    {
        public List<Question> Questions = [];

        public Question CreateQuestion(string questionText)
        {
            Question question = new(questionText);
            Questions.Add(question);
            return question;
        }

        public List<Question> RetrieveQuestions()
        {
            return Questions;
        }

        public void UpdateQuestion(Question updatedQuestion)
        {
            // Find the question to update (using ID, text, or other identifier)
            Question questionToUpdate = Questions.FirstOrDefault(q => q.Id == updatedQuestion.Id);

            if (questionToUpdate != null)
            {
                questionToUpdate.QuestionText = updatedQuestion.QuestionText;
                questionToUpdate.AnswerOptions = updatedQuestion.AnswerOptions;
                questionToUpdate.CorrectAnswer = updatedQuestion.CorrectAnswer;
            }
            else
            {
                Console.WriteLine($"Question not found with the id of: {updatedQuestion.Id}, couldnt updated question");
            }
        }

        public void DeleteQuestion(Question questionToDelete)
        {
            Question questionToRemove = Questions.FirstOrDefault(q => q.Id == questionToDelete.Id);
            if (questionToRemove != null)
            {
                Questions.Remove(questionToRemove);
            }
            else
            {
                Console.WriteLine($"Question not found with the id of: {questionToDelete.Id}");
            }
        }

        public void ImportListOfQuestionsFromJson(string json)
        {
            //Implement some more error handling
            List<Question> ImportedQuestions = JsonSerializer.Deserialize<List<Question>>(json);

            if (ImportedQuestions != null)
            {
                Questions = ImportedQuestions;

            }
            else
            {
                Console.WriteLine("Imported Questions where null, will be set to [] instead");
                // We never want the database to be null, will only complicate things down the line
                Questions = [];
            }

        }

        public string RetrieveQuestionsAsJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            return JsonSerializer.Serialize<List<Question>>(Questions, options);
        }

        public int Count()
        {
            return Questions.Count;
        }
    }
}