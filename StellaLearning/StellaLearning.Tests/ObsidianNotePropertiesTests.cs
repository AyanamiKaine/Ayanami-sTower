using StellaLearning.Util.NoteHandler;

namespace StellaLearning.Tests;

public class ObsidianNotePropertiesTests
{
    [Fact]
    public void ParseObsidianNotePropertiesUnitTest()
    {
        var obsidianNote =
        """
        ---
        Created: 2024-02-12 22:45
        aliases:
        - Why Victoria3 crashed on intel cpus
        tags:
        - Blog-Post
        ---
        """;


        var properties = ObsidianNoteProperties.Parse(obsidianNote);


        Assert.Equal(DateTime.Parse("2024-02-12 22:45"), properties.Created);
        Assert.Equal
        (
            ["Why Victoria3 crashed on intel cpus"],
            properties.Aliases
        );
        Assert.Equal
        (
            ["Blog-Post"],
            properties.Tags
        );
    }
}
