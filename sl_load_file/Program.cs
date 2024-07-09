// See https://aka.ms/new-console-template for more information
using LoadFilesToLearn;
using NetMQ;
using NetMQ.Sockets;
using System.Diagnostics;
using System.Text.Json;


Server server = new();
server.Run();