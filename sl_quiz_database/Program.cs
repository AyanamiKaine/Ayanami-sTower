using NetMQ;
using System.Text.Json;
using NetMQ.Sockets;
using sl_quiz_database;
using System.Collections.Generic;
using static NetMQ.NetMQSelector;


// Instead of doing this we could add the ability to parse the argument 'test'
// to run our unit tests
#if DEBUG // If we are not in debug mode than instead we run our tests.
    Console.WriteLine("Running in Debug mode");
    Console.WriteLine("Running Tests");
    
    QuestionTests test = new();
    QuestionTests.Run();

    QuizDatabaseTests quizDatabaseTests = new();
    quizDatabaseTests.Run();
#else 
    Server server = new();
    server.Run();
#endif 
