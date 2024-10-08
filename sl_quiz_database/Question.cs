namespace sl_quiz_database
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string QuestionText { get; set; } = "";
        public List<string> AnswerOptions { get; set; } = [];
        public string CorrectAnswer { get; set; } = "";
        public int Priority { get; set; } = 0;
        public DateTime NextReviewDate { get; set; } = DateTime.Now;
        public int NumberOfTimeSeen { get; set; } = 0;
        public Question()
        {
        }
        public Question(string questionText)
        {
            QuestionText = questionText;
        }


        public Question(Guid id, string questionText)
        {
            Id = id;
            QuestionText = questionText;
        }

        public Question(string guidId, string questionText)
        {
            if (Guid.TryParse(guidId, out Guid parsedId))
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

            AnswerOptions.Add(answerOption);
        }

        public bool IsCorrectAnswer(string anwser)
        {
            return anwser == CorrectAnswer;
        }
    }
}
