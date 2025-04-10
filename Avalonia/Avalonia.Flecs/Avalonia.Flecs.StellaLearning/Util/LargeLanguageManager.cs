/*
This should be a singleton that can be used in the entire app as a single place where 
we can call, large language models APIs and local large language models with just simple functions.
*/using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GenerativeAI; // Main namespace from the library
using GenerativeAI.Types;
using HtmlAgilityPack; // For Request/Response types if needed

namespace Avalonia.Flecs.StellaLearning.Util;

/// <summary>
/// Represents metadata information extracted from a URL, such as title and associated tags.
/// </summary>
public class UrlMetadata
{
    // Use System.Text.Json attributes if needed for specific naming,
    // but matching property names usually works.
    // [JsonPropertyName("Title")]
    /// <summary>
    /// AI Generated Title
    /// </summary>
    public string? Title { get; set; }
    /// <summary>
    /// AI Generated Author
    /// </summary>
    public string? Author { get; set; }
    // [JsonPropertyName("Tags")]
    /// <summary>
    /// AI Generated Tags
    /// </summary>
    public List<string>? Tags { get; set; }
    /// <summary>
    /// AI Generated Publisher
    /// </summary>
    public string? Publisher { get; set; }
    /// <summary>
    /// AI Generated Publisher
    /// </summary>
    public string? Summary { get; set; }

    /*
    TODO: 
    We should also add a date for when the information was created => Date.Now()
    and the date when the source was created, this would need to be created by the LLM
    */
}

/// <summary>
/// Singleton manager for interacting with large language models throughout the application.
/// Provides a centralized interface for calling language model APIs and local models with simple functions.
/// </summary>
public sealed partial class LargeLanguageManager
{
    // --- Singleton Implementation ---

    // Private static instance holder using Lazy<T> for thread-safety and lazy initialization
    private static readonly Lazy<LargeLanguageManager> lazyInstance =
        new Lazy<LargeLanguageManager>(() => new LargeLanguageManager());

    /// <summary>
    /// Gets the singleton instance of the LargeLanguageManager class.
    /// This property provides thread-safe, lazy-initialized access to the manager.
    /// </summary>
    public static LargeLanguageManager Instance => lazyInstance.Value;

    // --- Generative AI Client ---

    private readonly GenerativeModel _textModel;
    private readonly GenerativeModel _imageModel;

    private const string DefaultModel = GoogleAIModels.Gemini2FlashLitePreview; // Use a cost-effective and capable model
    private readonly HttpClient _httpClient;
    // Private constructor to prevent external instantiation

    [GeneratedRegex(@"(\r\n|\r|\n)")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MyRegex1();
    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MyRegex2();
    private LargeLanguageManager()
    {
        // --- Initialization ---
        // Read API Key from environment variable (Recommended approach)
        // Ensure you set the 'GEMINI_API' environment variable before running.
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API");

        if (string.IsNullOrEmpty(apiKey))
        {
            // You could also fall back to reading from a config file or other sources here
            throw new InvalidOperationException(
                "Gemini API Key not found. Set the GEMINI_API environment variable.");
        }

        _httpClient = new HttpClient();
        // Optional: Configure HttpClient (e.g., Timeout, Headers)
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyApp/1.0 (LanguageModelBot; +http://example.com/botinfo)"); // Good practice to set a User-Agent


        try
        {
            // Initialize the GoogleAI client using the API Key
            var googleAI = new GoogleAi(apiKey);

            // Create the GenerativeModel instance
            // You can make the model name configurable if needed
            _textModel = googleAI.CreateGenerativeModel(DefaultModel);
            _imageModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemini2Flash);

            Console.WriteLine($"LargeLanguageManager initialized with model: {DefaultModel}");
        }
        catch (Exception ex)
        {
            // Log the exception details appropriately in a real application
            Console.Error.WriteLine($"Failed to initialize GoogleAI or GenerativeModel: {ex.Message}");
            // Rethrow or handle appropriately depending on application requirements
            throw;
        }
    }

    // --- Public Methods ---

    /// <summary>
    /// Generates a text response based on a given text prompt.
    /// </summary>
    /// <param name="prompt">The text prompt to send to the model.</param>
    /// <returns>The generated text response as a string, or null if an error occurred.</returns>
    public async Task<string?> GetTextResponseAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Console.Error.WriteLine("Prompt cannot be null or empty.");
            return null;
        }

        try
        {
            // Use the simple GenerateContentAsync overload for text prompts
            var response = await _textModel.GenerateContentAsync(prompt);

            // Extract the text content from the response
            // The .Text() extension method is a convenient way provided by the library
            return response?.Text()?.Trim();
        }
        catch (Exception ex)
        {
            // Log the error in a real application (using a logging framework like Serilog, NLog, etc.)
            Console.Error.WriteLine($"Error generating text response for prompt '{prompt}': {ex.Message}");
            Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}"); // Include stack trace for debugging
            return null; // Return null to indicate failure
        }
    }

    /// <summary>
    /// Generates a list of tags describing the content of an image file.
    /// </summary>
    /// <param name="imagePath">The local file path to the image.</param>
    /// <param name="maxTags">Optional maximum number of tags to request.</param>
    /// <returns>A list of string tags, or an empty list if an error occurred or no tags were generated.</returns>
    public async Task<List<string>> GetImageTagsAsync(string imagePath, int maxTags = 4)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Console.Error.WriteLine("Image path cannot be null or empty.");
            return [];
        }

        if (!File.Exists(imagePath))
        {
            Console.Error.WriteLine($"Image file not found at path: {imagePath}");
            return [];
        }
        var request = new GenerateContentRequest();

        // Construct a prompt specifically asking for tags in a parseable format
        // You might need to experiment with this prompt for optimal results
        string prompt = $"""
        Describe this image with relevant THEME tags. THAT CAN BE USED TO CATEGORIES THE IMAGE
        SO THEIR TAGS CAN BE USED TO SEARCH FOR THE IMAGE, FOR EXAMPLE AN PAINTING OF AN APPLE
        WOULD GET THE TAG APPLE AND PAINTING, AND ITS STYLE. SEEING A PICTURE OF A WOMEN SMOKING
        WOULD BECOME WOMEN, SMOKING TAGS. ONLY RETURN THE TAGS LIST NOTHING ELSE
        Provide up to {maxTags} tags as a comma-separated list (e.g., tag1, tag2, tag3).
        """;

        request.AddText(prompt);

        try
        {
            // Attach a local file
            request.AddInlineFile(imagePath);

            // Use the GenerateContentAsync overload that takes a prompt and a local file path
            // The library handles reading the file and sending it appropriately.
            var response = await _imageModel.GenerateContentAsync(request);

            var generatedText = response?.Text()?.Trim();

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                Console.WriteLine($"No tags generated for image: {imagePath}");
                return [];
            }

            // Parse the comma-separated list
            return generatedText.Split(',')
                                    .Select(tag => tag.Trim()) // Remove leading/trailing whitespace
                                    .Where(tag => !string.IsNullOrEmpty(tag)) // Remove empty entries
                                    .ToList();
        }
        catch (Exception ex)
        {
            // Log the error
            Console.Error.WriteLine($"Error generating tags for image '{imagePath}': {ex.Message}");
            Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
            return []; // Return empty list on failure
        }
    }

    /// <summary>
    /// Fetches content from a URL, extracts text using HtmlAgilityPack,
    /// and sends it to the AI model along with a specific prompt.
    /// </summary>
    /// <param name="url">The URL of the website to fetch.</param>
    /// <param name="promptAboutUrlContent">The specific question or instruction about the website content.</param>
    /// <returns>The AI's response, trimmed, or null if an error occurs.</returns>
    public async Task<UrlMetadata?> GetResponseFromUrlAsync(string url, string promptAboutUrlContent)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            Console.Error.WriteLine($"Invalid URL provided: {url}");
            return null;
        }
        if (string.IsNullOrWhiteSpace(promptAboutUrlContent))
        {
            Console.Error.WriteLine("Prompt about URL content cannot be null or empty.");
            return null;
        }

        string htmlContent;
        try
        {
            Console.WriteLine($"Fetching content from: {url}");
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            htmlContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Successfully fetched {htmlContent.Length} characters (raw HTML) from {url}");
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"HTTP request error fetching URL '{url}': {ex.Message} (Status Code: {ex.StatusCode})");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            Console.Error.WriteLine($"Timeout fetching URL '{url}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching URL '{url}': {ex.Message}");
            return null;
        }

        // --- Start HTML Parsing and Text Extraction ---
        string cleanedText;
        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Remove potentially problematic or non-content nodes
            var nodesToRemove = htmlDoc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style" || n.Name == "nav" ||
                            n.Name == "footer" || n.Name == "header" || n.Name == "aside" ||
                            n.Name == "form" || n.Name == "button" || n.Name == "iframe")
                .ToList(); // Materialize before removing

            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }

            // Select the body node, or fallback to the document node
            var contentNode = htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode;

            // Get the inner text, which concatenates text from descendant nodes
            string rawInnerText = contentNode.InnerText;

            // Decode HTML entities (like &amp;, &lt;, &nbsp;)
            var decodedText = HtmlEntity.DeEntitize(rawInnerText);

            // Normalize whitespace:
            // 1. Replace various newline formats with a single standard newline \n
            var textWithStandardNewlines = MyRegex().Replace(decodedText, "\n");
            // 2. Replace 3 or more consecutive newlines with exactly two (like a paragraph break)
            var textWithParagraphs = MyRegex1().Replace(textWithStandardNewlines, "\n\n");
            // 3. Replace 2 or more spaces/tabs with a single space
            var textWithSingleSpaces = MyRegex2().Replace(textWithParagraphs, " ");
            // 4. Trim leading/trailing whitespace and newlines from the final result
            cleanedText = textWithSingleSpaces.Trim();

            Console.WriteLine($"Extracted and cleaned text: {cleanedText.Length} characters.");

            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                Console.Error.WriteLine($"Failed to extract meaningful text content from URL: {url}");
                return null; // Return null if extraction yielded nothing useful
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error parsing HTML or extracting text from URL '{url}': {ex.Message}");
            // Optional: Fallback to sending raw HTML or just return null
            return null;
        }
        // --- End HTML Parsing and Text Extraction ---


        // Prepare the combined prompt using the CLEANED text
        // Note: Changed prompt slightly to indicate it's extracted text, not raw HTML
        string finalPrompt = $"Here is the extracted text content from the website {url}:\n" +
                             $"---\n" + // Use simpler separator
                             $"{cleanedText}\n" +
                             $"---\n\n" +
                             $"Based on the text content above, please answer the following question:\n" +
                             $"{promptAboutUrlContent}";

        // Call the AI model (same logic as before)
        Console.WriteLine("Sending combined prompt (Extracted Text + Question) to AI model...");
        try
        {
            var aiResponse = await _textModel.GenerateContentAsync(finalPrompt);
            var metaData = aiResponse.ToObject<UrlMetadata>();
            var rawText = aiResponse?.Text();
            var trimmedText = rawText?.Trim();

            if (string.IsNullOrEmpty(trimmedText))
            {
                Console.WriteLine("AI model returned an empty response after trimming.");
                return null;
            }
            return metaData;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating AI response for URL '{url}': {ex.Message}");
            Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Fetches content from a URL, extracts text, and asks the AI model
    /// to generate a title and tags, returning them as a structured UrlMetadata object.
    /// </summary>
    /// <param name="url">The URL of the website to fetch.</param>
    /// <param name="maxTags">Maximum number of tags to request.</param>
    /// <returns>A UrlMetadata object containing the title and tags, or null if an error occurs.</returns>
    public async Task<UrlMetadata?> GetUrlMetadataAsync(string url, int maxTags = 5)
    {
        string? cleanedText = await FetchAndCleanHtmlAsync(url); // Use refactored helper
        if (cleanedText == null)
        {
            return null; // Error logged in helper
        }

        // Construct the prompt specifically asking for JSON output
        // matching the UrlMetadata structure. Be explicit!
        string jsonPrompt = $"""
                Generate a concise, relevant title for the content and a list of up to {maxTags} relevant keyword tags.

                Extracted Text:
                ---
                {cleanedText}
                ---

                Instructions:
                Respond ONLY with a valid JSON object containing the generated title and tags.
                The JSON object must have exactly two keys: "Title" (string) and "Tags" (array of strings).
                """;

        // Create the request object
        var request = new GenerateContentRequest();
        request.AddText(jsonPrompt);

        // Configure the request to expect JSON output (important!)
        // This tells the SDK/API to enforce JSON mode.
        request.UseJsonMode<UrlMetadata>();

        Console.WriteLine($"Sending request for JSON metadata (URL: {url}) to AI model...");
        try
        {
            // Use GenerateObjectAsync<T> to directly get the deserialized object
            UrlMetadata? result = await _textModel.GenerateObjectAsync<UrlMetadata>(request);

            if (result == null)
            {
                Console.WriteLine("AI model returned null or failed to deserialize the JSON object.");
            }
            else
            {
                Console.WriteLine($"Successfully received JSON metadata: Title='{result.Title}', Tags='{(result.Tags != null ? string.Join(", ", result.Tags) : "None")}'");
            }

            return result; // Return the deserialized object (or null if it failed)
        }
        catch (Exception ex)
        {
            // Log the specific error related to JSON generation/parsing
            Console.Error.WriteLine($"Error generating or parsing JSON metadata for URL '{url}': {ex.Message}");
            // Consider checking exception type for more specific API errors if available
            Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
            return null;
        }
    }

    // --- Helper Method for HTML Fetching and Cleaning --- (Refactored from GetResponseFromUrlAsync)
    private async Task<string?> FetchAndCleanHtmlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            Console.Error.WriteLine($"Invalid URL provided: {url}");
            return null;
        }

        string htmlContent;
        try
        {
            Console.WriteLine($"Fetching content from: {url}");
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            htmlContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Successfully fetched {htmlContent.Length} characters (raw HTML) from {url}");
        }
        catch (HttpRequestException ex) { Console.Error.WriteLine($"HTTP error fetching URL '{url}': {ex.Message}"); return null; }
        catch (TaskCanceledException ex) { Console.Error.WriteLine($"Timeout fetching URL '{url}': {ex.Message}"); return null; }
        catch (Exception ex) { Console.Error.WriteLine($"Error fetching URL '{url}': {ex.Message}"); return null; }

        string cleanedText;
        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            var nodesToRemove = htmlDoc.DocumentNode.Descendants().Where(n => /* ... node names ... */
    n.Name == "script" || n.Name == "style" || n.Name == "nav" || n.Name == "footer" || n.Name == "header" || n.Name == "aside" || n.Name == "form" || n.Name == "button" || n.Name == "iframe").ToList();
            foreach (var node in nodesToRemove) { node.Remove(); }
            var contentNode = htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode;
            string rawInnerText = contentNode.InnerText;
            var decodedText = HtmlEntity.DeEntitize(rawInnerText);
            var textWithStandardNewlines = MyRegex().Replace(decodedText, "\n"); // (\r\n|\r|\n)
            var textWithParagraphs = MyRegex1().Replace(textWithStandardNewlines, "\n\n"); // \n{3,}
            var textWithSingleSpaces = MyRegex2().Replace(textWithParagraphs, " "); // [ \t]{2,}
            cleanedText = textWithSingleSpaces.Trim();
            Console.WriteLine($"Extracted and cleaned text: {cleanedText.Length} characters.");
            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                Console.Error.WriteLine($"Failed to extract meaningful text content from URL: {url}");
                return null;
            }
            return cleanedText;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error parsing HTML for URL '{url}': {ex.Message}");
            return null;
        }
    }
}