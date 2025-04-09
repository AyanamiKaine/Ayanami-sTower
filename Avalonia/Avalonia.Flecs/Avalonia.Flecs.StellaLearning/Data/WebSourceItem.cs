using System;
using System.Text;
using System.Text.Json.Serialization;
using Avalonia.Controls.Chrome;
using Avalonia.Flecs.StellaLearning.Util;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.Flecs.StellaLearning.Data // Use your appropriate namespace
{
    /// <summary>
    /// Represents a literature source that is primarily accessed via a URL.
    /// </summary>
    public partial class WebSourceItem : LiteratureSourceItem
    {
        /// <summary>
        /// Gets or sets the URL for the online source.
        /// This field is non-nullable for this source type.
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("Url")]
        private string _url;

        /// <summary>
        /// Gets or sets the date the online source was accessed. Important for websites.
        /// </summary>
        [ObservableProperty]
        [JsonPropertyName("AccessedDate")]
        private DateTime? _accessedDate;

        // --- Constructor ---

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSourceItem"/> class.
        /// This constructor is intended for JSON deserialization.
        /// </summary>
        [JsonConstructor]
        public WebSourceItem() : base(LiteratureSourceType.Website)
        {
            _url = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSourceItem"/> class with the specified URL.
        /// </summary>
        /// <param name="url">The URL of the web source. Cannot be null or whitespace.</param>
        /// <param name="type">The type of the literature source. Defaults to Website.</param>
        /// <param name="name">Used as a name when the item gets displayed.</param>
        /// <exception cref="ArgumentException">Thrown when the URL is null or whitespace.</exception>
        public WebSourceItem(string url, LiteratureSourceType type = LiteratureSourceType.Website, string name = "") : base(type)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
            // Basic URL validation (can be improved)
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                Console.WriteLine($"Warning: Invalid URL format provided for WebSourceItem: {url}");
                // throw new ArgumentException("Invalid URL format.", nameof(url));
            }
            _url = url;
            Name = name;
            Title = name;
            
        }

        /// <summary>
        /// Helper to build the common parts of an APA citation for web sources.
        /// </summary>
        protected virtual void BuildApaStyleCitationBase(StringBuilder sb)
        {
            // Author(s) or Group Author
            if (!string.IsNullOrWhiteSpace(Author))
            {
                sb.Append(Author.Trim().TrimEnd('.').TrimEnd(',')).Append(". ");
            }
            else
            {
                // APA uses (n.d.) if no date is available
                // sb.Append("(n.d.). "); // Uncomment if adhering strictly
            }

            // Title (Italicize for standalone web pages/reports)
            if (!string.IsNullOrWhiteSpace(Title))
            {
                string formattedTitle = Title.Trim().TrimEnd('.');
                // Italicize standalone works like web pages or reports
                if (SourceType == LiteratureSourceType.Website || SourceType == LiteratureSourceType.Report)
                {
                    sb.Append('*').Append(formattedTitle).Append("*");
                }
                else // Assume non-italicized for articles found online (journal title is italicized later)
                {
                    sb.Append(formattedTitle);
                }
                sb.Append(". ");
            }

            // Publisher/Site Name (Often used for websites)
            if (SourceType == LiteratureSourceType.Website && !string.IsNullOrWhiteSpace(Publisher))
            {
                sb.Append(Publisher.Trim().TrimEnd('.')).Append(". ");
            }

            // URL - Always include for web sources
            if (!string.IsNullOrWhiteSpace(Url))
            {
                // APA 7 generally doesn't require "Retrieved from" unless the content is designed to change over time
                // and no DOI is available. Access date is also often omitted unless content is dynamic.
                if (SourceType == LiteratureSourceType.Website && AccessedDate.HasValue)
                {
                    // Example: Including retrieval date for potentially dynamic content
                    // sb.Append($"Retrieved {AccessedDate.Value:MMMM d, yyyy}, from ");
                }
                sb.Append(Url.Trim());
            }
        }
    }
}
