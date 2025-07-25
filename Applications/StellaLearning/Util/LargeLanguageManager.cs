/*
Stella Learning is a modern learning app.
Copyright (C) <2025>  <Patrick, Grohs>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
/*
This should be a singleton that can be used in the entire app as a single place where
we can call, large language models APIs and local large language models with just simple functions.
*/using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AyanamisTower.StellaLearning.Data; // For Request/Response types if needed
using Flecs.NET.Core;
using GenerativeAI; // Main namespace from the library
using GenerativeAI.Types;
using HtmlAgilityPack;

namespace AyanamisTower.StellaLearning.Util;

/*
While much of this logic was moved to the backend, i think its better to keep it in the client.
Why? Because this makes it much more easiert to integrate local llms. As well as sending
the data to gemini because then we dont have to store direct user data and can instead
implement end to end encryption. So the data we receive is never first not unencrypted.
*/

/// <summary>
/// Represents metadata information extracted from content (web, pdf, markdown, etc.), such as title and associated tags.
/// </summary>
public class ContentMetadata
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

    /// <summary>
    /// Returns a string representation of the ContentMetadata object.
    /// </summary>
    /// <returns>A formatted string containing the metadata properties.</returns>
    public override string ToString()
    {
        var tagsList = Tags != null ? string.Join(", ", Tags) : "No tags";

        return $"Title: {Title ?? "Unknown"}\n"
            + $"Author: {Author ?? "Unknown"}\n"
            + $"Publisher: {Publisher ?? "Unknown"}\n"
            + $"Tags: {tagsList}\n"
            + $"Summary: {Summary ?? "No summary"}";
    }

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
    // Efficient lookup for supported extensions (using HashSet for O(1) average time complexity)
    // Store extensions in lowercase including the dot for consistent comparison.
    private static readonly HashSet<string> _supportedFileApiExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Code Files
            ".c",
            ".cpp",
            ".py",
            ".java",
            ".php",
            ".sql",
            ".html",
            // Document Files
            ".doc",
            ".docx",
            ".pdf",
            ".rtf",
            ".dot",
            ".dotx",
            ".hwp",
            ".hwpx",
            // Plain Text
            ".txt",
            // Presentation
            ".pptx",
            // Spreadsheet/Tabular
            ".xls",
            ".xlsx",
            ".csv",
            ".tsv",
        };

    // Extensions we can reasonably convert to plain text for analysis
    private static readonly HashSet<string> _convertibleToTextExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".md",
            ".markdown", // Markdown
            ".log", // Log files
            ".xml", // XML
            ".json", // JSON (though API might support directly)
            ".yaml",
            ".yml", // YAML
            ".css", // CSS
            ".js", // JavaScript (if not using .py, .java etc.)
            ".sh", // Shell scripts
            ".bat" // Batch files
            ,
            // Add any other primarily text-based formats you encounter
        };

    // --- Singleton Implementation ---

    // Private static instance holder using Lazy<T> for thread-safety and lazy initialization// Make lazyInstance nullable and remove direct initialization here
    private static Lazy<LargeLanguageManager>? lazyInstance;

    // Lock object for thread-safe initialization
    private static readonly Lock padlock = new();

    /// <summary>
    /// Gets the singleton instance of the LargeLanguageManager class.
    /// Ensure that Initialize(world) has been called prior to accessing this property.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before Initialize has been called.</exception>
    public static LargeLanguageManager Instance
    {
        get
        {
            if (lazyInstance == null)
            {
                // This state should ideally not be reached if Initialize is called correctly at startup.
                // This check protects against incorrect usage patterns.
                throw new InvalidOperationException(
                    "LargeLanguageManager has not been initialized. Call Initialize(world) first during application startup."
                );
            }
            // Return the lazily initialized instance (the factory delegate inside Lazy<T> will run here if it hasn't already)
            return lazyInstance.Value;
        }
    }

    // --- Generative AI Client ---

    private readonly GeminiModel _textModel;
    private readonly GenerativeModel _imageModel;
    private readonly World _world;
    private readonly HttpClient _httpClient;

    // Private constructor to prevent external instantiation

    [GeneratedRegex(@"(\r\n|\r|\n)")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MyRegex1();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MyRegex2();

    private LargeLanguageManager(World world) // <--- Added parameter
    {
        // Assign the world instance immediately
        _world = world;

        // --- Initialization ---
        // Read API Key from environment variable (Recommended approach)
        // Ensure you set the 'GEMINI_API' environment variable before running.
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API");

        if (string.IsNullOrEmpty(apiKey))
        {
            // You could also fall back to reading from a config file or other sources here
            throw new InvalidOperationException(
                "Gemini API Key not found. Set the GEMINI_API environment variable."
            );
        }

        _httpClient = new HttpClient();
        // Optional: Configure HttpClient (e.g., Timeout, Headers)
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36"
        );

        try
        {
            // Initialize the GoogleAI client using the API Key
            var googleAI = new GoogleAi(apiKey);

            // Create the GenerativeModel instance
            // You can make the model name configurable if needed
            _textModel = googleAI.CreateGeminiModel(GoogleAIModels.Gemini2FlashExp);
            _imageModel = googleAI.CreateGenerativeModel(GoogleAIModels.Gemini2Flash);

            //Console.WriteLine($"LargeLanguageManager initialized with model: {DefaultModel}");
        }
        catch (Exception ex)
        {
            // Log the exception details appropriately in a real application
            Console.Error.WriteLine(
                $"Failed to initialize GoogleAI or GenerativeModel: {ex.Message}"
            );
            // Rethrow or handle appropriately depending on application requirements
            throw;
        }
    }

    /// <summary>
    /// Initializes the singleton instance of the LargeLanguageManager with the required World dependency.
    /// This method MUST be called once during application startup before accessing the Instance property.
    /// </summary>
    /// <param name="world">The application's Flecs.NET World instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided world instance is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if Initialize is called more than once.</exception>
    public static void Initialize(World world)
    {
        // Use double-checked locking for thread safety during initialization
        // Although typically called once at startup, this makes it robust.
        if (lazyInstance == null) // First check (avoids lock contention if already initialized)
        {
            lock (padlock)
            {
                if (lazyInstance == null) // Second check (ensures only one thread initializes)
                {
                    // Create the Lazy instance, passing the world to the constructor via the factory delegate
                    lazyInstance = new Lazy<LargeLanguageManager>(
                        () => new LargeLanguageManager(world)
                    );
                    Console.WriteLine("LargeLanguageManager initialized.");
                }
                else
                {
                    // Optional: Could log a warning here if another thread initialized between the checks.
                    // Or throw InvalidOperationException if strict single-call is desired even with race conditions.
                    // throw new InvalidOperationException("LargeLanguageManager has already been initialized by another thread.");
                }
            }
        }
        else
        {
            // If Initialize is called again after successful initialization
            throw new InvalidOperationException(
                "LargeLanguageManager has already been initialized. Initialize should only be called once."
            );
            // Alternatively, you could just log a warning and return if you want to allow redundant calls.
            // Console.WriteLine("Warning: LargeLanguageManager.Initialize called more than once.");
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
        if (!_world.Get<Settings>().EnableLargeLanguageFeatures)
        {
            return null;
        }

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
            Console.Error.WriteLine(
                $"Error generating text response for prompt '{prompt}': {ex.Message}"
            );
            Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}"); // Include stack trace for debugging
            return null; // Return null to indicate failure
        }
    }

    /// <summary>
    /// Generates metadata (title, author, tags, publisher, summary) for a given file using AI analysis.
    /// </summary>
    /// <param name="filePath">The path to the file to analyze.</param>
    /// <param name="maxTags">The maximum number of tags to generate (default is 4).</param>
    /// <param name="existingTags">A list of already existing tags</param>
    /// <returns>A ContentMetadata object containing the generated metadata, or null if an error occurred.</returns>
    public async Task<ContentMetadata?> GenerateMetaDataBasedOnFile(
        string filePath,
        List<string> existingTags,
        int maxTags = 4
    )
    {
        if (!_world.Get<Settings>().EnableLargeLanguageFeatures)
        {
            return null;
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine("file must exists");
            return null;
        }

        string? extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            Console.Error.WriteLine(
                $"Error: Could not determine file extension for '{filePath}'. Cannot process."
            );
            return null;
        }

        bool isDirectlySupported = _supportedFileApiExtensions.Contains(extension);
        bool isConvertible =
            !isDirectlySupported && _convertibleToTextExtensions.Contains(extension);

        string fileToUploadPath = filePath; // Default to original path
        string? tempFilePath = null; // Path for temporary file, if created
        bool useConversion = false;

        // --- Logic Branch: Conversion Fallback ---
        if (isConvertible)
        {
            Console.WriteLine(
                $"File type '{extension}' is not directly supported by File API, attempting conversion to .txt."
            );
            useConversion = true;
            try
            {
                // Create a unique temporary file path
                tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");

                // Read original content
                string originalContent = await File.ReadAllTextAsync(filePath);

                // Write to temporary .txt file
                await File.WriteAllTextAsync(tempFilePath, originalContent);

                Console.WriteLine(
                    $"Successfully converted '{Path.GetFileName(filePath)}' to temporary file: '{tempFilePath}'"
                );
                fileToUploadPath = tempFilePath; // Upload the temporary file
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"Error during temporary file conversion for '{filePath}': {ex.Message}"
                );
                // Clean up temp file if it was created partially before error
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                    }
                    catch
                    { /* Ignore delete error */
                    }
                }
                return null; // Cannot proceed if conversion fails
            }
        }
        // --- Logic Branch: Unsupported Type ---
        else if (!isDirectlySupported)
        {
            Console.Error.WriteLine(
                $"Error: File type '{extension}' is not directly supported by the File API and is not configured for text conversion. Skipping '{filePath}'."
            );
            return null;
        }

        RemoteFile? remoteFile = null;
        try
        {
            Console.WriteLine(
                $"Uploading {(useConversion ? "temporary " : "")}file '{Path.GetFileName(fileToUploadPath)}' (Original: '{Path.GetFileName(filePath)}') using File API..."
            );
            remoteFile = await _textModel.Files.UploadFileAsync(
                fileToUploadPath!,
                progressCallback: (progress) =>
                {
                    Console.WriteLine($"Upload Progress: {progress:F2}%");
                }
            );

            Console.WriteLine($"File uploaded successfully. Remote File Name: {remoteFile.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
            throw;
        }

        var request = new GenerateContentRequest
        {
            SystemInstruction = new GenerativeAI.Types.Content(
                "You are an AI assistant specialized in extracting structured metadata from text content. Your task is to analyze the provided text content below and generate a single, valid JSON object containing specific metadata fields.",
                "SYSTEM"
            ),
        };
        // Configure the request to expect JSON output (important!)
        // This tells the SDK/API to enforce JSON mode.
        //request.UseJsonMode<ContentMetadata>();

        // Construct a prompt specifically asking for tags in a parseable format
        // You might need to experiment with this prompt for optimal results
        var promptText = $"""
            Analyze Content: Carefully read the entire content
            Extract Metadata: Identify and extract the following pieces of information from the text:

            Title: Determine the most appropriate main title for the content. This should be concise and representative.
            Author: Identify the primary author(s) or creator(s) of the content. If multiple authors are present, list them if possible, but a single string representation is acceptable (e.g., "John Doe and Jane Smith").

            Tags: Generate a list of highly relevant keywords or topic tags that categorize the core subject matter of the content.
            Quantity: Generate {maxTags} distinct tags. Do not exceed 5 tags under any circumstances. NEVER GENERATE MORE THAN {maxTags} TAGS, NEVER.

            Here is a list of already {string.Join(", ", existingTags)} existing tags that you should prefer to pick an appropriate TAG IF AN APPROPRIATE TAG exists. If no appropriate tag exists
            generate your own.

            Relevance & Type: Tags should represent the main themes, concepts, or disciplines discussed. Prefer broader topics over hyper-specific details.
            Format: Tags should be single words or short phrases (ideally 1-2 words long) and always start with a capital letter (Math, Biology, Science, Software, Study).

            Exclusions: Do NOT include chapter titles, section headings, specific page numbers, minor character names, or generic words from the table of contents/index (like "Introduction", "Index", "Bibliography", "Acknowledgements", "One", "Two", "Three") as tags unless they represent a truly central theme.
            Publisher: Identify the entity (organization, company, website, individual) responsible for publishing or distributing the content.

            Summary: Generate a brief (2-6 sentence) summary capturing the core message or purpose of the content.
            Format Output: Structure the extracted information into a single, valid JSON object.
            The JSON object must use the following exact field names (keys): Title, Author, Tags, Publisher, Summary.
            """;

        request.AddText(promptText);
        request.AddRemoteFile(remoteFile!);

        //request.UseJsonMode<ContentMetadata>();

        // Call the AI model (same logic as before)
        Console.WriteLine("Sending combined prompt (Extracted Text + Question) to AI model...");
        try
        {
            var aiResponse = await _textModel.GenerateContentAsync(request);
            return aiResponse.ToObject<ContentMetadata>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Error generating AI response for file '{filePath}': {ex.Message}"
            );
            Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
            return null;
        }
        finally // --- Cleanup: ALWAYS try to delete the temporary file ---
        {
            if (tempFilePath != null && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    Console.WriteLine($"Successfully deleted temporary file: '{tempFilePath}'");
                }
                catch (IOException ioEx)
                {
                    // Log non-critically, the main operation might have succeeded
                    Console.Error.WriteLine(
                        $"Warning: Failed to delete temporary file '{tempFilePath}': {ioEx.Message}"
                    );
                }
            }
            // Optional: Delete the remote file if no longer needed (requires another API call)
            if (remoteFile != null)
            {
                try
                {
                    await _textModel.Files.DeleteFileAsync(remoteFile!.Name!);
                    Console.WriteLine($"Deleted remote file: {remoteFile.Name}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(
                        $"Warning: Failed to delete remote file '{remoteFile.Name}': {ex.Message}"
                    );
                }
            }
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
        if (!_world.Get<Settings>().EnableLargeLanguageFeatures)
        {
            return [];
        }

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
            return generatedText
                .Split(',')
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
    public async Task<ContentMetadata?> GetResponseFromUrlAsync(
        string url,
        string promptAboutUrlContent
    )
    {
        if (!_world.Get<Settings>().EnableLargeLanguageFeatures)
        {
            return null;
        }

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
            Console.WriteLine(
                $"Successfully fetched {htmlContent.Length} characters (raw HTML) from {url}"
            );
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine(
                $"HTTP request error fetching URL '{url}': {ex.Message} (Status Code: {ex.StatusCode})"
            );
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
            var nodesToRemove = htmlDoc
                .DocumentNode.Descendants()
                .Where(n =>
                    n.Name == "script"
                    || n.Name == "style"
                    || n.Name == "nav"
                    || n.Name == "footer"
                    || n.Name == "header"
                    || n.Name == "aside"
                    || n.Name == "form"
                    || n.Name == "button"
                    || n.Name == "iframe"
                )
                .ToList(); // Materialize before removing

            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }

            // Select the body node, or fallback to the document node
            var contentNode =
                htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode;

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
                Console.Error.WriteLine(
                    $"Failed to extract meaningful text content from URL: {url}"
                );
                return null; // Return null if extraction yielded nothing useful
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Error parsing HTML or extracting text from URL '{url}': {ex.Message}"
            );
            // Optional: Fallback to sending raw HTML or just return null
            return null;
        }
        // --- End HTML Parsing and Text Extraction ---


        // Prepare the combined prompt using the CLEANED text
        // Note: Changed prompt slightly to indicate it's extracted text, not raw HTML
        string finalPrompt =
            $"Here is the extracted text content from the website {url}:\n"
            + "---\n"
            + // Use simpler separator
            $"{cleanedText}\n"
            + "---\n\n"
            + "Based on the text content above, please answer the following question:\n"
            + $"{promptAboutUrlContent}";

        // Call the AI model (same logic as before)
        Console.WriteLine("Sending combined prompt (Extracted Text + Question) to AI model...");
        try
        {
            var aiResponse = await _textModel.GenerateContentAsync(finalPrompt);
            var metaData = aiResponse.ToObject<ContentMetadata>();

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
    public async Task<ContentMetadata?> GetUrlMetadataAsync(string url, int maxTags = 5)
    {
        if (!_world.Get<Settings>().EnableLargeLanguageFeatures)
        {
            return null;
        }

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
        request.UseJsonMode<ContentMetadata>();

        Console.WriteLine($"Sending request for JSON metadata (URL: {url}) to AI model...");
        try
        {
            // Use GenerateObjectAsync<T> to directly get the deserialized object
            ContentMetadata? result = await _textModel.GenerateObjectAsync<ContentMetadata>(
                request
            );

            if (result == null)
            {
                Console.WriteLine(
                    "AI model returned null or failed to deserialize the JSON object."
                );
            }
            else
            {
                Console.WriteLine(
                    $"Successfully received JSON metadata: Title='{result.Title}', Tags='{(result.Tags != null ? string.Join(", ", result.Tags) : "None")}'"
                );
            }

            return result; // Return the deserialized object (or null if it failed)
        }
        catch (Exception ex)
        {
            // Log the specific error related to JSON generation/parsing
            Console.Error.WriteLine(
                $"Error generating or parsing JSON metadata for URL '{url}': {ex.Message}"
            );
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
            Console.WriteLine(
                $"Successfully fetched {htmlContent.Length} characters (raw HTML) from {url}"
            );
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"HTTP error fetching URL '{url}': {ex.Message}");
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

        string cleanedText;
        try
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            var nodesToRemove = htmlDoc
                .DocumentNode.Descendants()
                .Where(n => /* ... node names ... */
                    n.Name == "script"
                    || n.Name == "style"
                    || n.Name == "nav"
                    || n.Name == "footer"
                    || n.Name == "header"
                    || n.Name == "aside"
                    || n.Name == "form"
                    || n.Name == "button"
                    || n.Name == "iframe"
                )
                .ToList();
            foreach (var node in nodesToRemove)
            {
                node.Remove();
            }
            var contentNode =
                htmlDoc.DocumentNode.SelectSingleNode("//body") ?? htmlDoc.DocumentNode;
            string rawInnerText = contentNode.InnerText;
            var decodedText = HtmlEntity.DeEntitize(rawInnerText);
            var textWithStandardNewlines = MyRegex().Replace(decodedText, "\n"); // (\r\n|\r|\n)
            var textWithParagraphs = MyRegex1().Replace(textWithStandardNewlines, "\n\n"); // \n{3,}
            var textWithSingleSpaces = MyRegex2().Replace(textWithParagraphs, " "); // [ \t]{2,}
            cleanedText = textWithSingleSpaces.Trim();
            Console.WriteLine($"Extracted and cleaned text: {cleanedText.Length} characters.");
            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                Console.Error.WriteLine(
                    $"Failed to extract meaningful text content from URL: {url}"
                );
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
