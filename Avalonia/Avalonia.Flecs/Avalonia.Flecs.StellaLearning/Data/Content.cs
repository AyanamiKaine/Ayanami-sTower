using System;
namespace Avalonia.Flecs.StellaLearning.Data;
/// <summary>
/// Represents the type of the content
/// </summary>
public enum ContentType
{
    /// <summary>
    /// The content is a file
    /// </summary>
    File,
    /// <summary>
    /// The content is a website
    /// </summary>
    Website,
    /// <summary>
    /// The content is an audio file
    /// </summary>
    Audio,
    /// <summary>
    /// The content is a video file
    /// </summary>
    Video,
    /// <summary>
    /// The content is a picture
    /// </summary>
    Picture,
    /// <summary>
    /// The content is a markdown file
    /// </summary>
    Markdown,
    /// <summary>
    /// The content is a text file
    /// </summary>
    Txt,
    /// <summary>
    /// The content is a PDF file
    /// </summary>
    PDF,
    /// <summary>
    /// The content is an executable
    /// </summary>
    Executable,
}

/// <summary>
/// Content
/// </summary>
/// <param name="name"></param>
/// <param name="shortDescription"></param>
/// <param name="longDescription"></param>
/// <param name="contentType"></param>
/// <param name="priority"></param>
public class Content(string name = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam", string shortDescription = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam", string longDescription = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.", ContentType contentType = ContentType.File, int priority = 0)
{
    /// <summary>
    /// Name of the content
    /// </summary>
    public string Name { get; set; } = name;
    /// <summary>
    /// Short description of the content, used for display purposes
    /// </summary>
    public string ShortDescription { get; set; } = shortDescription;
    /// <summary>
    /// Long description of the content
    /// </summary>
    public string LongDescription { get; set; } = longDescription;
    /// <summary>
    /// Priority of the content, in which order it should be consumed
    /// </summary>
    public int Priority { get; set; } = priority;
    /// <summary>
    /// Type of the content
    /// </summary>
    public ContentType ContentType { get; set; } = contentType;
    /// <summary>
    /// Number of times the content has been seen
    /// </summary>
    public int NumberOfTimesSeen { get; set; } = 0;
    /// <summary>
    /// When the content was added
    /// </summary>
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;


    /// <summary>
    /// Override the ToString method
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{Name} \"{ShortDescription}\" ({AddedDate.ToShortDateString()}) TYPE: {ContentType}";
    }
}