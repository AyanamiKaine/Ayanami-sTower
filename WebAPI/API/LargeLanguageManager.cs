/*
This should be a singleton that can be used in the entire app as a single place where 
we can call, large language models APIs and local large language models with just simple functions.
*/using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GenerativeAI; // Main namespace from the library
using GenerativeAI.Types;
using HtmlAgilityPack;
using AyanamisTower.WebAPI.Models;
using AyanamisTower.WebAPI.Util;

namespace AyanamisTower.WebAPI.API;

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
    private readonly byte[] _masterKey; // Add master key field

    private readonly bool _enableLargeLanguageFeatures;
    [GeneratedRegex(@"(\r\n|\r|\n)")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MyRegex1();
    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MyRegex2();
    /// <summary>
    /// Initializes a new instance of the <see cref="LargeLanguageManager"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClientFactory">The factory to create HttpClient instances.</param>
    /// <exception cref="InvalidOperationException">Thrown if essential configuration (API key, Master Key) is missing or invalid.</exception>
    public LargeLanguageManager(
           IConfiguration configuration,
           ILogger<LargeLanguageManager> logger,
           IHttpClientFactory httpClientFactory // Use IHttpClientFactory
                                                // If World/Settings *must* be accessed, try injecting them if registered in DI
                                                // ISettingsService settingsService // Example if settings are abstracted
           )
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("GeminiClient"); // Create a named client


        var masterKeyBase64 = _configuration["StorageSettings:MasterEncryptionKey"];
        if (string.IsNullOrEmpty(masterKeyBase64))
        {
            _logger.LogCritical("MasterEncryptionKey (Base64) is missing in configuration.");
            throw new InvalidOperationException("MasterEncryptionKey is not configured correctly.");
        }
        try
        {
            _masterKey = Convert.FromBase64String(masterKeyBase64);
            if (_masterKey.Length != 32) // Ensure 256-bit key
            {
                _logger.LogCritical("Decoded MasterEncryptionKey is not 32 bytes (256 bits) long.");
                throw new InvalidOperationException("MasterEncryptionKey must be a Base64 encoded 256-bit key.");
            }
            _logger.LogInformation("Master encryption key loaded successfully."); // Add confirmation
        }
        catch (FormatException ex)
        {
            _logger.LogCritical(ex, "MasterEncryptionKey is not a valid Base64 string.");
            throw new InvalidOperationException("MasterEncryptionKey is not valid Base64.", ex);
        }

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
        _enableLargeLanguageFeatures = _configuration.GetValue("Features:EnableLargeLanguage", true); // Default to true if not set?

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

    /// <summary>
    /// Generates metadata for a file by analyzing its content using AI.
    /// </summary>
    /// <param name="fileMetadata">The metadata object containing information about the file to analyze.</param>
    /// <param name="maxTags">Maximum number of tags to generate (default is 4).</param>
    /// <returns>A ContentMetadata object containing generated title, author, tags, publisher, and summary, or null if processing fails.</returns>
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


        // --- Decryption Setup ---
        byte[] decryptedFileKey;
        byte[] fileContentIv;
        try
        {
            _logger.LogDebug("Decoding encryption keys/IVs for File ID {FileId}", fileMetadata.Id);
            byte[] encryptedKeyBytes = Convert.FromBase64String(fileMetadata.EncryptedFileKey);
            fileContentIv = Convert.FromBase64String(fileMetadata.EncryptionIV);

            _logger.LogDebug("Decrypting per-file key using master key for File ID {FileId}", fileMetadata.Id);
            // Use the shared helper to decrypt the per-file key
            decryptedFileKey = EncryptionHelper.DecryptData(_masterKey, encryptedKeyBytes);
            _logger.LogInformation("Successfully decrypted file key for File ID {FileId}", fileMetadata.Id);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to decode Base64 encryption key/IV for File ID {FileId}", fileMetadata.Id);
            return null;
        }
        catch (CryptographicException cryptoEx)
        {
            _logger.LogError(cryptoEx, "Failed to decrypt file key using master key for File ID {FileId}. Master key might be incorrect or data corrupted.", fileMetadata.Id);
            return null;
        }
        catch (Exception ex) // Catch broader exceptions during setup
        {
            _logger.LogError(ex, "Unexpected error during decryption setup for File ID {FileId}", fileMetadata.Id);
            return null;
        }
        // --- End Decryption Setup ---

        string? extension = Path.GetExtension(fileMetadata.StoredPath)?.ToLowerInvariant() ?? "";
        bool canExtractText = _convertibleToTextExtensions.Contains(extension ?? "") || (extension == ".txt");

        string? tempDecryptedFilePath = null; // Path if we have to save decrypted content
        RemoteFile? remoteFile = null; // For File API upload
        bool useFileApi = _supportedFileApiExtensions.Contains(extension ?? ""); // Check if File API supports the original type

        try
        {
            // Create Decryption Stream
            using var encryptedStream = new FileStream(fileMetadata.StoredPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var aes = Aes.Create();
            aes.Key = decryptedFileKey;
            aes.IV = fileContentIv;
            using var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

            if (useFileApi) // File API preferred for its native support
            {
                // Option A: Save decrypted content to temp file for File API
                tempDecryptedFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(fileMetadata.OriginalFileName)}"); // Use original extension
                _logger.LogInformation("Decrypting file {FileId} to temporary path {TempPath} for File API upload.", fileMetadata.Id, tempDecryptedFilePath);
                using (var tempStream = new FileStream(tempDecryptedFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await cryptoStream.CopyToAsync(tempStream);
                }
                _logger.LogInformation("Temporary decrypted file created successfully.");

                // Upload the TEMPORARY DECRYPTED file
                _logger.LogInformation("Uploading temporary decrypted file '{FileName}' using File API (File ID: {FileId})", Path.GetFileName(tempDecryptedFilePath), fileMetadata.Id);
                remoteFile = await _textModel.Files.UploadFileAsync(
                    tempDecryptedFilePath
                );
                _logger.LogInformation("Temporary decrypted file uploaded successfully. Remote File Name: {RemoteFileName}", remoteFile.Name);

                // --- Construct Prompt for File API ---
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
                // request.UseJsonMode<ContentMetadata>();

                _logger.LogInformation("Sending request to AI model using File API (File ID: {FileId})", fileMetadata.Id);
                var aiResponse = await _textModel.GenerateContentAsync(request); // Or GenerateObjectAsync
                return aiResponse.ToObject<ContentMetadata>();

            }
            else if (canExtractText) // Try text extraction directly from CryptoStream
            {
                _logger.LogInformation("Attempting direct text extraction from decrypted stream for File ID {FileId}", fileMetadata.Id);
                // For plain text:
                if (extension == ".txt" || _convertibleToTextExtensions.Contains(extension ?? ""))
                {
                    using var reader = new StreamReader(cryptoStream); // CryptoStream is the decrypted stream
                    string extractedText = await reader.ReadToEndAsync();
                    _logger.LogInformation("Extracted {Length} characters from decrypted text stream for File ID {FileId}", extractedText.Length, fileMetadata.Id);

                    // TODO: Add extraction logic for other text types if needed (e.g., PDF, DOCX)
                    // Example for PDF (requires PdfPig NuGet package):
                    // using UglyToad.PdfPig;
                    // using var pdfDoc = PdfDocument.Open(cryptoStream); // Process CryptoStream directly
                    // string extractedText = string.Join(" ", pdfDoc.GetPages().Select(p => p.Text));

                    if (string.IsNullOrWhiteSpace(extractedText))
                    {
                        _logger.LogWarning("Text extraction yielded empty content for File ID {FileId}", fileMetadata.Id);
                        return null;
                    }

                    // --- Construct Prompt for Text ---
                    string textPrompt = $"""
                        Analyze the following extracted text content.
                        Generate a concise title, author, publisher, summary, and up to {maxTags} relevant keyword tags.

                        Extracted Text:
                        ---
                        {extractedText}
                        ---

                        Instructions:
                        Respond ONLY with a single, valid JSON object.
                        The JSON object must use the exact field names: "Title", "Author", "Tags", "Publisher", "Summary".
                        "Tags" must be an array of strings. All other fields should be strings. If a field cannot be determined, use null or an empty string.
                        """;
                    var textRequest = new GenerateContentRequest();
                    textRequest.AddText(textPrompt);
                    textRequest.UseJsonMode<ContentMetadata>();

                    _logger.LogInformation("Sending request to AI model using extracted text (File ID: {FileId})", fileMetadata.Id);
                    ContentMetadata? result = await _textModel.GenerateObjectAsync<ContentMetadata>(textRequest);
                    return result;
                }
                else
                {
                    _logger.LogWarning("Text extraction not implemented for file type '{Extension}' for File ID {FileId}", extension, fileMetadata.Id);
                    return null; // Or fallback to temp file if desired
                }
            }
            else
            {
                _logger.LogWarning("File type '{Extension}' for File ID {FileId} is not supported by File API and not configured for text extraction.", extension, fileMetadata.Id);
                return null;
            }
        }
        catch (CryptographicException cryptoEx)
        {
            _logger.LogError(cryptoEx, "Cryptography error during file decryption for File ID {FileId}", fileMetadata.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI processing or decryption for File ID '{FileId}'", fileMetadata.Id);
            return null;
        }
        finally
        {
            // Securely clear decrypted key
            if (decryptedFileKey != null) Array.Clear(decryptedFileKey, 0, decryptedFileKey.Length);

            // --- Cleanup ---
            if (tempDecryptedFilePath != null && File.Exists(tempDecryptedFilePath))
            {
                try { File.Delete(tempDecryptedFilePath); _logger.LogInformation("Deleted temporary decrypted file: {TempPath}", tempDecryptedFilePath); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temporary decrypted file: {TempPath}", tempDecryptedFilePath); }
            }
            if (remoteFile != null)
            {
                try { await _textModel.Files.DeleteFileAsync(remoteFile.Name!); _logger.LogInformation("Deleted remote file: {RemoteFileName}", remoteFile.Name); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete remote file: {RemoteFileName}", remoteFile.Name); }
            }
        }
    }

    /// <summary>
    /// Generates a list of tags describing the content of an image file.
    /// </summary>
    /// <param name="fileMetadata">The metadata of the image file to analyze.</param>
    /// <param name="maxTags">Optional maximum number of tags to request.</param>
    /// <returns>A list of string tags, or an empty list if an error occurred or no tags were generated.</returns>
    public async Task<List<string>> GetImageTagsAsync(FileMetadata fileMetadata, int maxTags = 4) // Changed signature
    {
        if (!_enableLargeLanguageFeatures) { /* ... */ return []; }
        if (string.IsNullOrEmpty(fileMetadata.StoredPath)) { /* ... */ return []; }
        if (!File.Exists(fileMetadata.StoredPath)) { /* ... */ return []; }

        // --- Decryption Setup ---
        byte[] decryptedFileKey;
        byte[] fileContentIv;
        try
        {
            _logger.LogDebug("Decoding/Decrypting keys for image tag generation (File ID {FileId})", fileMetadata.Id);
            byte[] encryptedKeyBytes = Convert.FromBase64String(fileMetadata.EncryptedFileKey);
            fileContentIv = Convert.FromBase64String(fileMetadata.EncryptionIV);
            decryptedFileKey = EncryptionHelper.DecryptData(_masterKey, encryptedKeyBytes);
            _logger.LogInformation("Successfully decrypted file key for image (File ID {FileId})", fileMetadata.Id);
        }
        catch (Exception ex) // Catch format, crypto, etc.
        {
            _logger.LogError(ex, "Failed decryption setup for image tag generation (File ID {FileId})", fileMetadata.Id);
            return [];
        }
        // --- End Decryption Setup ---

        byte[] decryptedImageBytes;
        try
        {
            // Create Decryption Stream and read fully into memory
            _logger.LogDebug("Opening encrypted image file {StoredPath}", fileMetadata.StoredPath);
            using var encryptedStream = new FileStream(fileMetadata.StoredPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var aes = Aes.Create();
            aes.Key = decryptedFileKey;
            aes.IV = fileContentIv;
            _logger.LogDebug("Creating crypto stream for image {FileId}", fileMetadata.Id);
            using var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var memoryStream = new MemoryStream();
            _logger.LogDebug("Reading decrypted image data into memory for {FileId}", fileMetadata.Id);
            await cryptoStream.CopyToAsync(memoryStream);
            decryptedImageBytes = memoryStream.ToArray();
            _logger.LogInformation("Successfully decrypted image data ({Length} bytes) for File ID {FileId}", decryptedImageBytes.Length, fileMetadata.Id);
        }
        catch (CryptographicException cryptoEx)
        {
            _logger.LogError(cryptoEx, "Cryptography error during image file decryption for File ID {FileId}", fileMetadata.Id);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading or decrypting image file for File ID {FileId}", fileMetadata.Id);
            return [];
        }
        finally
        {
            // Securely clear decrypted key
            if (decryptedFileKey != null) Array.Clear(decryptedFileKey, 0, decryptedFileKey.Length);
        }

        try
        {
            var request = new GenerateContentRequest();


            string prompt = $"""
        Describe this image with relevant THEME tags. THAT CAN BE USED TO CATEGORIES THE IMAGE
        SO THEIR TAGS CAN BE USED TO SEARCH FOR THE IMAGE, FOR EXAMPLE AN PAINTING OF AN APPLE
        WOULD GET THE TAG APPLE AND PAINTING, AND ITS STYLE. SEEING A PICTURE OF A WOMEN SMOKING
        WOULD BECOME WOMEN, SMOKING TAGS. ONLY RETURN THE TAGS LIST NOTHING ELSE
        Provide up to {maxTags} tags as a comma-separated list (e.g., tag1, tag2, tag3).
        """;

            request.AddText(prompt);

            // Use AddInlineData with the decrypted byte array
            string imageDataBase64 = Convert.ToBase64String(decryptedImageBytes);

            request.AddInlineData(imageDataBase64, fileMetadata.ContentType ?? "image/jpeg", true, "user"); // Use stored content type, fallback if null

            _logger.LogInformation("Sending request for image tags using inline decrypted data (File ID: {FileId})", fileMetadata.Id);
            var response = await _imageModel.GenerateContentAsync(request);
            var generatedText = response?.Text()?.Trim();

            if (string.IsNullOrWhiteSpace(generatedText)) { /* ... */ return []; }

            return generatedText.Split(',').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending decrypted image data or generating tags for File ID '{FileId}'", fileMetadata.Id);
            return [];
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
    public async Task<ContentMetadata?> GenerateMetaDataFromUrlAsync(string url, int maxTags = 5)
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

        string jsonPrompt = $"""
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
        // Create the request object

        var request = new GenerateContentRequest();


        string finalPrompt = $"Here is the extracted text content from the website {url}:\n" +
                             $"---\n" + // Use simpler separator
                             $"{cleanedText}\n" +
                             $"---\n\n" +
                             $"Based on the text content above, please answer the follow the following:\n" +
                             $"{jsonPrompt}";


        request.AddText(finalPrompt);

        // Configure the request to expect JSON output (important!)
        // This tells the SDK/API to enforce JSON mode.
        //request.UseJsonMode<ContentMetadata>();

        _logger.LogInformation($"Sending request for JSON metadata (URL: {url}) to AI model...");
        try
        {
            // Use GenerateObjectAsync<T> to directly get the deserialized object

            var aiResponse = await _textModel.GenerateContentAsync(finalPrompt);
            var result = aiResponse.ToObject<ContentMetadata>();

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