using NetMQ;
using System.Text.Json;
using NetMQ.Sockets;
using Spaced_Repetition_Database;
using System.Collections.Generic;
using static NetMQ.NetMQSelector;

#if DEBUG
// Run your tests only if in Debug mode 
#else
                // Server = new();
                //Server.Run();
#endif