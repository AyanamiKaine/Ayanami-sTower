using Stella.Testing;

namespace sl_quiz_database
{
    class QuizDatabaseTests
    {

        [ST_TEST]
        private static StellaTesting.TestingResult CreateQuestionTest()
        {
            QuizDatabase quizDatabase = new();

            Question createdQuestion = quizDatabase.
                                        CreateQuestion("What is the capital of france?");

            createdQuestion.AddAnswerOption("Paris", true);


            return StellaTesting.AssertEqual(1, quizDatabase.Count(), "Expected Number of entries is 1");
        }

        [ST_TEST]
        private static StellaTesting.TestingResult CreatedQuestionsAnwserShouldBeParis()
        {
            QuizDatabase quizDatabase = new();

            Question createdQuestion = quizDatabase.
                                        CreateQuestion("What is the capital of france?");

            createdQuestion.AddAnswerOption("Paris", true);

            return StellaTesting.AssertEqual("Paris", quizDatabase.Questions[0].CorrectAnswer);
        }

        [ST_TEST]
        private static StellaTesting.TestingResult CreatedQuestionsShouldHaveRightQuestion()
        {
            QuizDatabase quizDatabase = new();

            Question createdQuestion = quizDatabase.
                                        CreateQuestion("What is the capital of france?");

            createdQuestion.AddAnswerOption("Paris", true);

            return StellaTesting.AssertEqual("What is the capital of france?", quizDatabase.Questions[0].QuestionText);
        }

        [ST_TEST]
        private static StellaTesting.TestingResult AddingAnwserToAQuestionForATotalof3()
        {
            QuizDatabase quizDatabase = new();

            Question createdQuestion = quizDatabase.
                                        CreateQuestion("What is the capital of france?");

            createdQuestion.AddAnswerOption("Paris", true);

            Question updatedQuestion = new(createdQuestion.Id, "What is the capital of france?");
            updatedQuestion.AddAnswerOption("Paris", true);
            updatedQuestion.AddAnswerOption("Moskau", false);
            updatedQuestion.AddAnswerOption("Berlin", false);


            quizDatabase.UpdateQuestion(updatedQuestion);

            return StellaTesting.AssertEqual(3, createdQuestion.AnswerOptions.Count, "Anwser Count should not be 3 anymore");
        }

        [ST_TEST]
        private static StellaTesting.TestingResult DeleteQuestionFromDatabase()
        {
            QuizDatabase quizDatabase = new();

            Question createdQuestion = quizDatabase.CreateQuestion("What is the name of my cat?");
            quizDatabase.DeleteQuestion(createdQuestion);

            return StellaTesting.AssertEqual(0, quizDatabase.Count(), "Question Database should be empty");
        }

        [ST_TEST]
        private static StellaTesting.TestingResult QuestionJsonRepresentationShouldBeAsExpected()
        {
            QuizDatabase quizDatabase = new();
            Question createdQuestion = quizDatabase.
                            CreateQuestion("What is the capital of france?");

            createdQuestion.AddAnswerOption("Paris", true);
            string expectedJson =
            """
            
            """;

            string actualJson = quizDatabase.RetrieveQuestionsAsJson();

            return StellaTesting.AssertEqual(expectedJson, actualJson, "Serialization of the question database is not as expected");
        }

        [ST_TEST]
        private static StellaTesting.TestingResult DesirializeJsonIntoDatabaseShouldIncreaseDatabaseByOne()
        {
            QuizDatabase quizDatabase = new();

            string json =
            """
            [
                { 
                "Id": "8f183cfe-8d01-4aad-b683-391bbaf921da",
                "QuestionText": "What is the capital of france?",
                "AnswerOptions": [
                    "Paris"
                ],
                "CorrectAnswer": "Paris",
                "Priority": 0,
                "NextReviewDate": "2024-07-26T21:37:28.0909555+02:00",
                "NumberOfTimeSeen": 0
                }
            ]
            """;

            quizDatabase.ImportListOfQuestionsFromJson(json);

            return StellaTesting.AssertEqual(1, quizDatabase.Count(), "Question Database should be one after deserializing");
        }
    }
}