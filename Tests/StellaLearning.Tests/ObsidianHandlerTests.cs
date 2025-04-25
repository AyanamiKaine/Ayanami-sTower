using AyanamisTower.StellaLearning.Util.NoteHandler;

namespace AyanamisTower.StellaLearning.Tests;

/// <summary>
/// Contains tests for the <see cref="ObsidianHandler"/> class.
/// </summary>
public class ObsidianHandlerTests
{
    /// <summary>
    /// Tests the parsing of an Obsidian vault.
    /// </summary>
    [Fact]
    public void ParseVaultTest()
    {
        // Here we are using my personal vault
        // TODO: We should create an example test vault.
        const string vaultPath = @"C:\Users\ayanami\AllTheKnowledgeReloaded";
        var list = ObsidianHandler.ParseVault(vaultPath, true);

        Assert.NotEmpty(list);
    }
}
