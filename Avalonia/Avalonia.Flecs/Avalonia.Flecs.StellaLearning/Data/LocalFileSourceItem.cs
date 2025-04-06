using System;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Avalonia.Flecs.StellaLearning.Data // Use your appropriate namespace
{
    /// <summary>
    /// Represents a literature source that is primarily associated with a local file.
    /// </summary>
    public partial class LocalFileSourceItem : LiteratureSourceItem
    {
        /// <summary>
        /// Gets or sets the full file path to the associated local file (e.g., PDF).
        /// This field is non-nullable for this source type.
        /// </summary>
        [ObservableProperty]
        private string _filePath; // Non-nullable

        // --- Constructor ---
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSourceItem"/> class.
        /// </summary>
        /// <param name="filePath">The full path to the local file. Cannot be null or empty.</param>
        /// <param name="type">The type of literature source. Defaults to <see cref="LiteratureSourceType.LocalFile"/>.</param>
        public LocalFileSourceItem(string filePath, LiteratureSourceType type = LiteratureSourceType.LocalFile) : base(type)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
            if (!File.Exists(filePath)) // Basic validation
            {
                Console.WriteLine($"Warning: File specified for LocalFileSourceItem does not exist: {filePath}");
                // Consider throwing an exception or handling this more robustly depending on requirements.
                // throw new FileNotFoundException("The specified file path does not exist.", filePath);
            }
            _filePath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath); // Default name from file
        }

        /// <summary>
        /// Helper to build the common parts of an APA citation. Can be called by derived classes.
        /// </summary>
        protected virtual void BuildApaStyleCitationBase(StringBuilder sb)
        {
            // Author(s)
            if (!string.IsNullOrWhiteSpace(Author))
            {
                sb.Append(Author.Trim().TrimEnd('.').TrimEnd(',')).Append(". ");
            }

            // Title (Assume Title Case for Books/Reports, needs adjustment for articles if inheriting)
            if (!string.IsNullOrWhiteSpace(Title))
            {
                string formattedTitle = Title.Trim().TrimEnd('.');
                // Italicize book/report titles in APA
                if (SourceType == LiteratureSourceType.Book || SourceType == LiteratureSourceType.Report || SourceType == LiteratureSourceType.Thesis)
                {
                    sb.Append('*').Append(formattedTitle).Append("*");
                }
                else
                {
                    sb.Append(formattedTitle); // Non-italicized for generic local file? Adjust as needed.
                }
                sb.Append(". ");
            }

            // Publisher (Common for Books/Reports)
            if (!string.IsNullOrWhiteSpace(Publisher))
            {
                sb.Append(Publisher.Trim().TrimEnd('.')).Append(". ");
            }

            // DOI (Less common for local files unless it's an article PDF)
            if (!string.IsNullOrWhiteSpace(Doi))
            {
                string formattedDoi = Doi.Trim();
                if (!formattedDoi.StartsWith("http"))
                {
                    sb.Append($"https://doi.org/{formattedDoi}");
                }
                else
                {
                    sb.Append(formattedDoi);
                }
            }
            // No URL or AccessedDate typically for local files unless it's a web download backup.
        }
    }
}
