using StellaLearning.Util.NoteHandler;

namespace StellaLearning.Tests;

public class ObsidianHandlerTests
{
    [Fact]
    public void ParseVaultTest()
    {
        var vaultPath = @"C:\Users\ayanami\AllTheKnowledgeReloaded";
        var list = ObsidianHandler.ParseVault(vaultPath, true);

        Assert.NotEmpty(list);
    }
}
