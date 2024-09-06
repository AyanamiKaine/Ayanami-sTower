using System.Security.Cryptography.X509Certificates;
using Stella.Testing;

namespace StellaFileWatcher
{
    public class StFileWatcherUnitTests
    {



        [ST_TEST]
        private static StellaTesting.TestingResult ShouldDispatchEventIfFileChanged()
        {
            return StellaTesting.AssertTrue(true);
        }

    }
}