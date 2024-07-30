using System.Diagnostics;
using SlBadRecall;
// See https://aka.ms/new-console-template for more information
// Check if the current build configuration is Debug
#if DEBUG
// Run your tests only if in Debug mode 
Stella.Testing.StellaTesting.RunTests();
#else
    StellaSRABad Server = new();
    Server.Run();
#endif