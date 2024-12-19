using System;
namespace Avalonia.Flecs.StellaLearning.Data;
public enum ContentType
{
    File,
    Website,
    Audio,
    Video,
    Picture,
    Markdown,
    Txt,
    PDF,
    Executable,
}

//Content represents an item that can be consumed for later time
public class Content(string name = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam", string shortDescription = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam", string longDescription = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.", ContentType contentType = ContentType.File, int priority = 0)
{
    public string Name { get; set; } = name;
    public string ShortDescription { get; set; } = shortDescription;
    public string LongDescription { get; set; } = longDescription;
    public int Priority { get; set; } = priority;
    public ContentType ContentType { get; set; } = contentType;
    public int NumberOfTimesSeen { get; set; } = 0;
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"{Name} \"{ShortDescription}\" ({AddedDate.ToShortDateString()}) TYPE: {ContentType}";
    }
}