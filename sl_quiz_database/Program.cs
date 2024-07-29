using NetMQ;
using System.Text.Json;
using NetMQ.Sockets;
using sl_quiz_database;
using System.Collections.Generic;
using static NetMQ.NetMQSelector;
using Stella.Testing;

// Instead of doing this we could add the ability to parse the argument 'test'
// to run our unit tests
#if DEBUG // If we are not in debug mode than instead we run our tests.
StellaTesting.RunTests();
#else
    Server server = new();
    server.Run();
#endif
// Note about TDD and the delivered product.
/*
I think that the program delivered to a user should be able to run the tests of the
program. 

Why?

Because the expected behavior and the tested behavior are highly linked together.
Of course only because tests run all ok does not mean that there are no bugs.
It only shows that the things we test work. (And this does not mean that the test
does not have a bug!)

I think there is a great value in being able to have tests that can be run by the user. 
*/