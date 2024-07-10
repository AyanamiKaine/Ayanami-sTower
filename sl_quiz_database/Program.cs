using NetMQ;
using System.Text.Json;
using NetMQ.Sockets;
using sl_quiz_database;
using System.Collections.Generic;
using static NetMQ.NetMQSelector;


#if DEBUG // Code enclosed in this block will only run in Debug mode
    Console.WriteLine("Running in Debug mode");
    Console.WriteLine("Running Tests");
    Test test = new();
Test.Run();
#else // Code enclosed in this block will only run in Release mode
    Server server = new();
    server.Run();
#endif 
