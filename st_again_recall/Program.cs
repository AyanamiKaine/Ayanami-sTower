using System.Diagnostics;
using SlAgainRecall;
// See https://aka.ms/new-console-template for more information
// Check if the current build configuration is Debug
#if DEBUG
// Run your tests only if in Debug mode 
Stella.Testing.StellaTesting.RunTests();
#else
    StellaSRAAgain Server = new();
    Server.Run();
#endif