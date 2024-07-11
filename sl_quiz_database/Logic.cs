using System.Text.Json;

namespace sl_quiz_database
{
    public class Logic
    {
        public void CreateQuestion(List<Question> questions, string questionText)
        {
            Question question = new(questionText);
            questions.Add(question);
        }

        public List<Question>? RetrieveQuestion(List<Question> questions)
        {
            if (questions.Count == 0)
            {
                return null; // Or throw an exception
            }
            // For simplicity, always return the first question
            return questions; 
        }

        public void DeleteQuestion(List<Question> questions, string questionText)
        {
            Question questionToRemove = questions.FirstOrDefault(q => q.QuestionText == questionText);
            if (questionToRemove != null)
            {
                questions.Remove(questionToRemove);
            }
            else
            {
                Console.WriteLine($"Question not found: {questionText}");
            }
        }

        public void UpdateQuestion(List<Question> questions, string questionData)
        {
            // Deserialize the updated question data (JSON)
            Question updatedQuestion = JsonSerializer.Deserialize<Question>(questionData);

            // Find the question to update (using ID, text, or other identifier)
            Question questionToUpdate = questions.FirstOrDefault(q => q.QuestionText == updatedQuestion.QuestionText);

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
