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
using GenerativeAI; // Main namespace from the library
using GenerativeAI.Types;
using HtmlAgilityPack;
using StellaLearningBackend.Data;
using StellaLearningBackend.Models;

namespace StellaLearningBackend.API;

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

        return $"Title: {Title ?? "Unknown"}\n" +
               $"Author: {Author ?? "Unknown"}\n" +
               $"Publisher: {Publisher ?? "Unknown"}\n" +
               $"Tags: {tagsList}\n" +
               $"Summary: {Summary ?? "No summary"}";
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
public partial class LargeLanguageManager // Removed 'sealed'
{

    // Efficient lookup for supported extensions (using HashSet for O(1) average time complexity)
    // Store extensions in lowercase including the dot for consistent comparison.
    private static readonly HashSet<string> _supportedFileApiExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Code Files
        ".c", ".cpp", ".py", ".java", ".php", ".sql", ".html",
        // Document Files
        ".doc", ".docx", ".pdf", ".rtf", ".dot", ".dotx", ".hwp", ".hwpx",
        // Plain Text
        ".txt",
        // Presentation
        ".pptx",
        // Spreadsheet/Tabular
        ".xls", ".xlsx", ".csv", ".tsv"
    };

    // Extensions we can reasonably convert to plain text for analysis
    private static readonly HashSet<string> _convertibleToTextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md", ".markdown", // Markdown
        ".log",           // Log files
        ".xml",           // XML
        ".json",          // JSON (though API might support directly)
        ".yaml", ".yml",  // YAML
        ".css",           // CSS
        ".js",            // JavaScript (if not using .py, .java etc.)
        ".sh",            // Shell scripts
        ".bat"            // Batch files
        // Add any other primarily text-based formats you encounter
    };

    // --- Generative AI Client ---

    private readonly GeminiModel _textModel;
    private readonly GenerativeModel _imageModel;
    private readonly HttpClient _httpClient;
    // Private constructor to prevent external instantiation
    private readonly IConfiguration _configuration;
    private readonly ILogger<LargeLanguageManager> _logger;
    private readonly ApplicationDbContext _context;
    private readonly bool _enableLargeLanguageFeatures;
    [GeneratedRegex(@"(\r\n|\r|\n)")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MyRegex1();
    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MyRegex2();
    public LargeLanguageManager(
           IConfiguration configuration,
           ILogger<LargeLanguageManager> logger,
           IHttpClientFactory httpClientFactory,
           ApplicationDbContext context // Use IHttpClientFactory
                                        // If World/Settings *must* be accessed, try injecting them if registered in DI
                                        // ISettingsService settingsService // Example if settings are abstracted
           )
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("GeminiClient"); // Create a named client
        _context = context;
        // --- Read Configuration ---
        // Read API Key from Configuration (more flexible than only Env Var)
        // Order: Environment Variable > Configuration File (appsettings, user secrets, etc.)
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API") ?? _configuration["Gemini:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogCritical("Gemini API Key not found. Set GEMINI_API environment variable or Gemini:ApiKey in configuration.");
            throw new InvalidOperationException("Gemini API Key not configured.");
        }

        // Read feature flag from Configuration instead of World/Settings
        _enableLargeLanguageFeatures = _configuration.GetValue<bool>("Features:EnableLargeLanguage", true); // Default to true if not set?

        // Configure HttpClient User-Agent (can be done centrally when registering the client)
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (StellaLearningBackend)");

        // --- Initialize AI Clients ---
        try
        {
            var googleAI = new GoogleAi(apiKey);
            // Read model names from configuration for flexibility
            var textModelName = _configuration["Gemini:TextModel"] ?? GoogleAIModels.Gemini2FlashExp;
            var imageModelName = _configuration["Gemini:ImageModel"] ?? GoogleAIModels.Gemini2Flash;

            _textModel = googleAI.CreateGeminiModel(textModelName);
            _imageModel = googleAI.CreateGenerativeModel(imageModelName);

            _logger.LogInformation("LargeLanguageManager initialized with TextModel: {TextModel}, ImageModel: {ImageModel}", textModelName, imageModelName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize GoogleAI or GenerativeModel.");
            throw; // Re-throw to prevent application from starting in a bad state
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
        if (!_enableLargeLanguageFeatures) // Check internal flag
        {
            _logger.LogWarning("Large language features are disabled. Skipping GetTextResponseAsync.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            _logger.LogError("Prompt cannot be null or empty.");
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
            _logger.LogError($"Error generating text response for prompt '{prompt}': {ex.Message}");
            _logger.LogError($"Stack Trace: {ex.StackTrace}"); // Include stack trace for debugging
            return null; // Return null to indicate failure
        }
    }

    public async Task<ContentMetadata?> GenerateMetaDataBasedOnFileAsync(FileMetadata fileMetadata, int maxTags = 4)
    {
        if (!_enableLargeLanguageFeatures)
        {
            _logger.LogWarning("Large language features are disabled. Skipping GenerateMetaDataBasedOnFile.");
            return null;
        }

        // Use the secure path directly from the provided metadata object
        string secureFilePath = fileMetadata.StoredPath;

        if (string.IsNullOrEmpty(secureFilePath))
        {
            _logger.LogError("StoredPath is missing in FileMetadata for ID {FileId}", fileMetadata.Id);
            return null; // Or handle appropriately
        }
        if (!File.Exists(secureFilePath)) // Check existence using the secure path
        {
            _logger.LogError("Secure file path does not exist: {SecureFilePath} (File ID: {FileId})", secureFilePath, fileMetadata.Id);
            // This might indicate an inconsistency between the DB and the file system.
            return null;
        }

        string? extension = Path.GetExtension(secureFilePath)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            _logger.LogError("Error: Could not determine file extension for secure path '{SecureFilePath}'.", secureFilePath);
            return null;
        }

        bool isDirectlySupported = _supportedFileApiExtensions.Contains(extension);
        bool isConvertible = !isDirectlySupported && _convertibleToTextExtensions.Contains(extension);

        string fileToUploadPath = secureFilePath; // Start with the secure path
        string? tempFilePath = null;          // Path for temporary file, if created
        bool useConversion = false;

        // --- Logic Branch: Conversion Fallback ---
        if (isConvertible)
        {
            _logger.LogInformation("File type '{Extension}' for File ID {FileId} not directly supported, attempting conversion to .txt.", extension, fileMetadata.Id);
            useConversion = true;
            try
            {
                tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
                // Read from the SECURE original file path
                string originalContent = await File.ReadAllTextAsync(secureFilePath);
                await File.WriteAllTextAsync(tempFilePath, originalContent);
                _logger.LogInformation("Successfully converted File ID {FileId} ('{OriginalFileName}') to temporary file: '{TempFilePath}'", fileMetadata.Id, fileMetadata.OriginalFileName, tempFilePath);
                fileToUploadPath = tempFilePath; // Upload the temporary file
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during temporary file conversion for File ID {FileId} (Path: {SecureFilePath})", fileMetadata.Id, secureFilePath);
                if (tempFilePath != null && File.Exists(tempFilePath)) { try { File.Delete(tempFilePath); } catch { /* Ignore */ } }
                return null;
            }
        }
        // --- Logic Branch: Unsupported Type ---
        else if (!isDirectlySupported)
        {
            _logger.LogError("Error: File type '{Extension}' for File ID {FileId} is not supported or convertible.", extension, fileMetadata.Id);
            return null;
        }

        RemoteFile? remoteFile = null;

        // --- Logic Branch: Conversion Fallback ---
        if (isConvertible)
        {
            _logger.LogInformation("File type '{Extension}' for File ID {FileId} not directly supported, attempting conversion to .txt.", extension, fileMetadata.Id);
            useConversion = true;
            try
            {
                tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
                // Read from the SECURE original file path
                string originalContent = await File.ReadAllTextAsync(secureFilePath);
                await File.WriteAllTextAsync(tempFilePath, originalContent);
                _logger.LogInformation("Successfully converted File ID {FileId} ('{OriginalFileName}') to temporary file: '{TempFilePath}'", fileMetadata.Id, fileMetadata.OriginalFileName, tempFilePath);
                fileToUploadPath = tempFilePath; // Upload the temporary file
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during temporary file conversion for File ID {FileId} (Path: {SecureFilePath})", fileMetadata.Id, secureFilePath);
                if (tempFilePath != null && File.Exists(tempFilePath)) { try { File.Delete(tempFilePath); } catch { /* Ignore */ } }
                return null;
            }
        }
        // --- Logic Branch: Unsupported Type ---
        else if (!isDirectlySupported)
        {
            _logger.LogError("Error: File type '{Extension}' for File ID {FileId} is not supported or convertible.", extension, fileMetadata.Id);
            return null;
        }

        try
        {
            _logger.LogInformation("Uploading {FileType} file '{FileName}' using File API (File ID: {FileId})",
                        useConversion ? "temporary" : "original", Path.GetFileName(fileToUploadPath), fileMetadata.Id);

            // Use the actual file path (secure or temporary) for the upload API call
            remoteFile = await _textModel.Files.UploadFileAsync(
                fileToUploadPath!,
                progressCallback: (progress) => { /* Log progress if needed */ }
            );
            _logger.LogInformation("File uploaded successfully. Remote File Name: {RemoteFileName} (File ID: {FileId})", remoteFile.Name, fileMetadata.Id);

        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Error uploading file: {ex.Message}");
        }

        var request = new GenerateContentRequest
        {
            SystemInstruction = new Content("You are an AI assistant specialized in extracting structured metadata from text content. Your task is to analyze the provided text content below and generate a single, valid JSON object containing specific metadata fields.", "SYSTEM")
        };
        // Configure the request to expect JSON output (important!)
        // This tells the SDK/API to enforce JSON mode.
        //request.UseJsonMode<ContentMetadata>();

        // Construct a prompt specifically asking for tags in a parseable format
        // You might need to experiment with this prompt for optimal results
        var promptText =
            $"""
            Analyze Content: Carefully read the entire content
            Extract Metadata: Identify and extract the following pieces of information from the text:

            Title: Determine the most appropriate main title for the content. This should be concise and representative.
            Author: Identify the primary author(s) or creator(s) of the content. If multiple authors are present, list them if possible, but a single string representation is acceptable (e.g., "John Doe and Jane Smith").

            Tags: Generate a list of highly relevant keywords or topic tags that categorize the core subject matter of the content.
            Quantity: Generate {maxTags} distinct tags. Do not exceed 5 tags under any circumstances. NEVER GENERATE MORE THAN {maxTags} TAGS, NEVER.

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
        _logger.LogInformation("Sending combined prompt (Extracted Text + Question) to AI model...");
        try
        {
            var aiResponse = await _textModel.GenerateContentAsync(request);
            return aiResponse.ToObject<ContentMetadata>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Stack Trace: {ex.StackTrace}");
            return null;
        }
        finally // --- Cleanup: ALWAYS try to delete the temporary file ---
        {
            if (tempFilePath != null && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    _logger.LogInformation($"Successfully deleted temporary file: '{tempFilePath}'");
                }
                catch (IOException ioEx)
                {
                    // Log non-critically, the main operation might have succeeded
                    _logger.LogError($"Warning: Failed to delete temporary file '{tempFilePath}': {ioEx.Message}");
                }
            }
            // Optional: Delete the remote file if no longer needed (requires another API call)
            if (remoteFile != null)
            {
                try { await _textModel.Files.DeleteFileAsync(remoteFile!.Name!); _logger.LogInformation($"Deleted remote file: {remoteFile.Name}"); }
                catch (Exception ex) { _logger.LogError($"Warning: Failed to delete remote file '{remoteFile.Name}': {ex.Message}"); }
            }
        }
    }

    /// <summary>
    /// Generates a list of tags describing the content of an image file.
    /// </summary>
    /// <param name="imagePath">The local file path to the image.</param>
    /// <param name="maxTags">Optional maximum number of tags to request.</param>
    /// <returns>A list of string tags, or an empty list if an error occurred or no tags were generated.</returns>
    public async Task<List<string>> GetImageTagsAsync(FileMetadata fileMetadata, int maxTags = 4) // Changed signature
    {
        if (!_enableLargeLanguageFeatures)
        {
            _logger.LogWarning("Large language features are disabled. Skipping GetImageTagsAsync.");
            return [];
        }

        string secureFilePath = fileMetadata.StoredPath;

        if (string.IsNullOrEmpty(secureFilePath))
        {
            _logger.LogError("StoredPath is missing in FileMetadata for ID {FileId}", fileMetadata.Id);
            return [];
        }
        if (!File.Exists(secureFilePath))
        {
            _logger.LogError("Secure file path does not exist: {SecureFilePath} (File ID: {FileId})", secureFilePath, fileMetadata.Id);
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
            request.AddInlineFile(secureFilePath); // Provide content type

            // Use the GenerateContentAsync overload that takes a prompt and a local file path
            // The library handles reading the file and sending it appropriately.
            var response = await _imageModel.GenerateContentAsync(request);

            var generatedText = response?.Text()?.Trim();

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                _logger.LogWarning("No tags generated for image: {FileId}", fileMetadata.Id);
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
            _logger.LogError(ex, "Error generating tags for image File ID '{FileId}'", fileMetadata.Id);
            _logger.LogError($"Stack Trace: {ex.StackTrace}");
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
    public async Task<ContentMetadata?> GetResponseFromUrlAsync(string url, string promptAboutUrlContent)
    {

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            _logger.LogError($"Invalid URL provided: {url}");
            return null;
        }
        if (string.IsNullOrWhiteSpace(promptAboutUrlContent))
        {
            _logger.LogError("Prompt about URL content cannot be null or empty.");
            return null;
        }

        string htmlContent;
        try
        {
            _logger.LogInformation($"Fetching content from: {url}");
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            htmlContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Successfully fetched {htmlContent.Length} characters (raw HTML) from {url}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request error fetching URL '{url}': {ex.Message} (Status Code: {ex.StatusCode})");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError($"Timeout fetching URL '{url}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching URL '{url}': {ex.Message}");
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

            _logger.LogInformation($"Extracted and cleaned text: {cleanedText.Length} characters.");

            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                _logger.LogError($"Failed to extract meaningful text content from URL: {url}");
                return null; // Return null if extraction yielded nothing useful
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing HTML or extracting text from URL '{url}': {ex.Message}");
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
        _logger.LogInformation("Sending combined prompt (Extracted Text + Question) to AI model...");
        try
        {
            var aiResponse = await _textModel.GenerateContentAsync(finalPrompt);
            var metaData = aiResponse.ToObject<ContentMetadata>();

            return metaData;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating AI response for URL '{url}': {ex.Message}");
            _logger.LogError($"Stack Trace: {ex.StackTrace}");
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
        if (!_enableLargeLanguageFeatures)
        {
            _logger.LogWarning("Large language features are disabled. Skipping GenerateMetaDataFromUrlAsync.");
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

        _logger.LogInformation($"Sending request for JSON metadata (URL: {url}) to AI model...");
        try
        {
            // Use GenerateObjectAsync<T> to directly get the deserialized object
            ContentMetadata? result = await _textModel.GenerateObjectAsync<ContentMetadata>(request);

            if (result == null)
            {
                _logger.LogInformation("AI model returned null or failed to deserialize the JSON object.");
            }
            else
            {
                _logger.LogInformation($"Successfully received JSON metadata: Title='{result.Title}', Tags='{(result.Tags != null ? string.Join(", ", result.Tags) : "None")}'");
            }

            return result; // Return the deserialized object (or null if it failed)
        }
        catch (Exception ex)
        {
            // Log the specific error related to JSON generation/parsing
            _logger.LogError($"Error generating or parsing JSON metadata for URL '{url}': {ex.Message}");
            // Consider checking exception type for more specific API errors if available
            _logger.LogError($"Stack Trace: {ex.StackTrace}");
            return null;
        }
    }

    // --- Helper Method for HTML Fetching and Cleaning --- (Refactored from GetResponseFromUrlAsync)
    private async Task<string?> FetchAndCleanHtmlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            _logger.LogError($"Invalid URL provided: {url}");
            return null;
        }

        string htmlContent;
        try
        {
            _logger.LogInformation($"Fetching content from: {url}");
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            htmlContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Successfully fetched {htmlContent.Length} characters (raw HTML) from {url}");
        }
        catch (HttpRequestException ex) { _logger.LogError($"HTTP error fetching URL '{url}': {ex.Message}"); return null; }
        catch (TaskCanceledException ex) { _logger.LogError($"Timeout fetching URL '{url}': {ex.Message}"); return null; }
        catch (Exception ex) { _logger.LogError($"Error fetching URL '{url}': {ex.Message}"); return null; }

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
            _logger.LogInformation($"Extracted and cleaned text: {cleanedText.Length} characters.");
            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                _logger.LogError($"Failed to extract meaningful text content from URL: {url}");
                return null;
            }
            return cleanedText;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing HTML for URL '{url}': {ex.Message}");
            return null;
        }
    }
}