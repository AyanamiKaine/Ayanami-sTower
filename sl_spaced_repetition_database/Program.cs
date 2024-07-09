using NetMQ;
using System.Text.Json;
using NetMQ.Sockets;
using Spaced_Repetition_Database;
using System.Collections.Generic;
using static NetMQ.NetMQSelector;

Server server = new();

server.Run();

