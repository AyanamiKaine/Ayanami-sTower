using AyanamisTower.StellaLearning.Util.NoteHandler;

namespace AyanamisTower.StellaLearning.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ObsidianNoteProperties"/> class.
/// </summary>
public class ObsidianNotePropertiesTests
{
    /// <summary>
    /// Tests the parsing of Obsidian note properties from a string.
    /// </summary>
    [Fact]
    public void ParseObsidianNotePropertiesUnitTest()
    {
        var obsidianNote = """
            ---
            Created: 2024-02-12 22:45
            aliases:
            - Why Victoria3 crashed on intel cpus
            tags:
            - Blog-Post
            ---
            There was a big problem in victoria 3 when you ran it on 12/13 gen intel cpus with performance and efficiency cores
            - Why ? because efficiency cores cant executes AVX 512 instructions! but performance cores can! the compiled code of victoria simply didnt expect that it cant run a specific piece of code when the OS schedules the thread on a cpu that CANT EXECUTE THE INSTRUCTIONS! 
            - https://youtu.be/kZCPURMH744?si=jRN6Zw7XXHiYeETE&t=4740
            """;

        var properties = ObsidianNoteProperties.Parse(obsidianNote);

        Assert.Equal(DateTime.Parse("2024-02-12 22:45"), properties.Created);
        Assert.Equal(["Why Victoria3 crashed on intel cpus"], properties.Aliases);
        Assert.Equal(["Blog-Post"], properties.Tags);
    }
}
