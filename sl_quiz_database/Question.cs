namespace sl_quiz_database
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid(); 
        public string QuestionText { get; set; } = "";
        public List<string> AnswerOptions { get; set; } = [];
        public string CorrectAnswer = "";
        public Question(string questionText)
        {
            Console.WriteLine($"Create Question with the text of {questionText}");
            QuestionText = questionText;
        }

        public Question(string guidId, string questionText)
        {
            Guid parsedId = new();
            if (Guid.TryParse(guidId, out parsedId))
            {
                Id = parsedId;
            }
            QuestionText = questionText;
        }

        public void AddAnswerOption(string answerOption, bool isCorrectAnswer)
        {
            if (string.IsNullOrWhiteSpace(answerOption))
            {
                throw new ArgumentException("Answer option cannot be null, empty, or whitespace.", nameof(answerOption));
            }
        
            if (isCorrectAnswer == true) 
            {
                CorrectAnswer = answerOption;
            }

            Console.WriteLine($"Adding anwser option: {answerOption} to the question of {QuestionText}");
            AnswerOptions.Add(answerOption);
        }

        public bool IsCorrectAnswer(string anwser)
        {
            if (anwser == CorrectAnswer) 
            {
                return true;
            }

            else 
            {
                return false;
            }
        }
    }
}
