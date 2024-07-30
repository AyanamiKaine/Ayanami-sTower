using System.Diagnostics;
using SlGoodRecall;
// See https://aka.ms/new-console-template for more information
// Check if the current build configuration is Debug
#if DEBUG
// Run your tests only if in Debug mode 
Stella.Testing.StellaTesting.RunTests();
#else
    StellaSRAGood Server = new();
    Server.Run();
#endif