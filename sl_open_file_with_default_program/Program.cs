using System.Diagnostics;
using System.IO;
using System.Text.Json;
using NetMQ;
using NetMQ.Sockets;
using OpenFileWithDefaultProgramComponent;

Server server = new();
server.Run();