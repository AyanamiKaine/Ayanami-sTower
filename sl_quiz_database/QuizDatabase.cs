using System.Text.Json;

namespace sl_quiz_database
{
    public class QuizDatabase
    {
        public List<Question> Questions = [];

        public void CreateQuestion(string questionText)
        {
            Question question = new(questionText);
            Questions.Add(question);
        }

        public List<Question>? RetrieveQuestions()
        {
            if (Questions.Count == 0)
            {
                return null; // Or throw an exception
            }
            // For simplicity, always return the first question
            return Questions; 
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

        public void UpdateQuestion(Question updatedQuestion)
        {
            // Find the question to update (using ID, text, or other identifier)
            Question questionToUpdate = Questions.FirstOrDefault(q => q.Id == updatedQuestion.Id);

            if (questionToUpdate != null)
            {
                // Update the properties of the existing question
                questionToUpdate = updatedQuestion;
            }
            else
            {
                Console.WriteLine($"Question not found with the id of: {updatedQuestion.Id}, couldnt updated question");
            }
        }

        public void ImportListOfQuestionsFromJson(string json)
        {

            //Implement some more error handling

            Questions = JsonSerializer.Deserialize<List<Question>>(json);

        }

        public string RetrieveQuestionsAsJson()
        {
            return JsonSerializer.Serialize<List<Question>>(Questions);
        }
    }
}