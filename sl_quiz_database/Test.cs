namespace sl_quiz_database
{

    /* 
    Here we write a simply set of test case, in spirite of TDD
    Why no TDD framework?, the tests should be really simple
    and most TDD frameworks for C# work with a second Test project
    that seperates tests from the implementation project. While 
    this might be a good idea (classic case of separation of concerns)
    I believe that having a test right next to the implementation much 
    better as a test(expected behavior) is highly coupled with the 
    implementation (actual behavior).

    But i aknowledge that this way of writing tests will not scale in the 
    long run but helps developing/guiding the design of the implemantation.
    */
    class Test
    {
        public static void Run()
        {

            if (QuestionCreation() == false)
            {
                Console.WriteLine("Question Creation Failed [X]");
            }

            if(AddingAnswerOption() == false)
            {
                Console.WriteLine("Adding Answer Option Failed [X]");
            }

        }

        private static bool QuestionCreation() 
        {
            Console.WriteLine("Running Test QuestionCreation");
            

            string questionText = "What is the capital of paris?";
            Question question = new(questionText);

            if (question.QuestionText != questionText)
            {
                return false;
            }


            Console.WriteLine("Question was successfully created [✓]\n");
            return true;
        }

        private static bool AddingAnswerOption()
        {
            Console.WriteLine("Running Test AddingAnswerOption");


            string questionText = "What is the capital of paris?";
            Question question = new(questionText);

            string answer1 = "Paris";
            string answer2 = "Berlin";
            string answer3 = "Prag";
            string answer4 = "Moskau";

            try
            {
                question.AddAnswerOption(answer1, true);
                question.AddAnswerOption(answer2, false);
                question.AddAnswerOption(answer3, false);
                question.AddAnswerOption(answer4, false);
            }
            catch(Exception e) 
            {
                Console.WriteLine(e.Message);
                return false;
            }

            if (question.AnswerOptions[0] != answer1 || 
                question.AnswerOptions[1] != answer2 || 
                question.AnswerOptions[2] != answer3 || 
                question.AnswerOptions[3] != answer4)
                {
                    return false;
                }

            Console.WriteLine("Answers were successfully added to a question [✓]\n");
            return true;
        }
    }

}