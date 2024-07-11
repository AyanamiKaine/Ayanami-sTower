namespace sl_quiz_database
{
    class QuizDatabaseTests
    {
        public void Run()
        {
            if (CreateQuestionTest() == false)
            {
                Console.WriteLine("Creating Question Entry in the Database failed [X]");
            }

            if (UpdateQuestionsTest() == false)
            {
                Console.WriteLine("Updaing question in the database failed [X]");
            }
        }


        private static bool CreateQuestionTest()
        {
            Console.WriteLine("Running Test CreateQuestion in the database");

            QuizDatabase quizDatabase = new();

            Question createdQuestion = quizDatabase.
                                        CreateQuestion("What is the capital of france?");
            
            createdQuestion.AddAnswerOption("Paris", true);


            if (quizDatabase.Count() != 1)
            {
                Console.WriteLine($"Expected Number of entries is 1, actual {quizDatabase.Count()}");
                return false;
            }

            if (quizDatabase.Questions[0].CorrectAnswer != "Paris")
            {
                Console.WriteLine($"The first entry was expected to hav thee correct answer of Paris, actual {quizDatabase.Questions[0].CorrectAnswer}");
                return false;
            }

            if (quizDatabase.Questions[0].QuestionText != "What is the capital of france?")
            {
                Console.WriteLine($"Question text was not as expected, expected: What is the capital of france?, actual: {quizDatabase.Questions[0].QuestionText}");
                return false;
            }

            if (quizDatabase.Questions[0].AnswerOptions[0]  != "Paris")
            {
                Console.WriteLine($"Question answer[0] was not as expected, expected: Paris, actual: {quizDatabase.Questions[0].AnswerOptions[0]}");
                return false;
            }


            Console.WriteLine("Question was successfully created in the database [✓]\n");
            return true;
        }   

        private static bool UpdateQuestionsTest()
        {
            Console.WriteLine("Running Test UpdateQuestions");

            QuizDatabase quizDatabase = new();

            Question createdQuestion = quizDatabase.
                                        CreateQuestion("What is the capital of france?");
            
            createdQuestion.AddAnswerOption("Paris", true);

            Question updatedQuestion = new(createdQuestion.Id, "What is the capital of france?");
            updatedQuestion.AddAnswerOption("Paris", true);
            updatedQuestion.AddAnswerOption("Moskau", false);
            updatedQuestion.AddAnswerOption("Berlin", false);


            quizDatabase.UpdateQuestion(updatedQuestion);

            if (createdQuestion.AnswerOptions.Count != 3)
            {
                Console.WriteLine($"Expected new count number of answer options was 3, actual:{createdQuestion.AnswerOptions.Count}");
                return false;
            }

            if (createdQuestion.QuestionText != "What is the capital of france?")
            {
                Console.WriteLine($"Expected QuestionText was What is the capital of france?, actual: ${updatedQuestion.QuestionText}");
                return false;
            }

            if (createdQuestion.CorrectAnswer != "Paris")
            {
                return false;
            }

            Console.WriteLine("Question was succesfully updated in the database [✓]\n");

            return true;
        }
    }
}