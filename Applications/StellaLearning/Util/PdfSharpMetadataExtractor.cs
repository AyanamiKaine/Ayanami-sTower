/*
<one line to give the program's name and a brief idea of what it does.>
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
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.IO;

namespace AyanamisTower.StellaLearning.Util;

/// <summary>
/// Extracts metadata from PDF files using the PdfSharp library.
///</summary>
public static class PdfSharpMetadataExtractor
{
    /// <summary>
    /// Extracts metadata from a PDF file at the specified path.
    /// </summary>
    /// <param name="pdfFilePath">The path to the PDF file.</param>
    /// <returns>A PdfMetadata object containing the extracted metadata.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified PDF file is not found.</exception>
    public static PdfMetadata ExtractMetadata(string pdfFilePath)
    {
        // PdfSharp requires this encoding registration for .NET Core / .NET 5+
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        if (!File.Exists(pdfFilePath))
        {
            Console.WriteLine($"Error: File not found at {pdfFilePath}");
            throw new FileNotFoundException();
        }

        PdfMetadata metadataInfo = new();

        // Open the PDF document
        // PdfReader.Open can take a password if needed
        using (PdfDocument document = PdfReader.Open(pdfFilePath, PdfDocumentOpenMode.Import))
        {
            // Access the document information dictionary
            PdfDocumentInformation info = document.Info;

            // Extract metadata fields
            metadataInfo.Title = info.Title ?? "";
            metadataInfo.Author = info.Author ?? "";
            metadataInfo.Subject = info.Subject ?? "";
            metadataInfo.Keywords = info.Keywords ?? "";
            metadataInfo.Creator = info.Creator ?? "";
            metadataInfo.Producer = info.Producer ?? "";
            metadataInfo.PageCount = document.PageCount;
            // PdfSharp provides DateTime objects directly, handling parsing internally
            metadataInfo.CreationDateTime = info.CreationDate;
            metadataInfo.ModificationDateTime = info.ModificationDate;


            Console.WriteLine($"Successfully extracted metadata from {pdfFilePath}");
        }

        return metadataInfo;
    }
}

/// <summary>
/// Represents metadata extracted from a PDF file.
/// </summary>
public class PdfMetadata
{
    /// <summary>
    /// Gets or sets the title of the PDF document.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Gets or sets the author of the PDF document.
    /// </summary>
    public string Author { get; set; } = "";

    /// <summary>
    /// Gets or sets the subject of the PDF document.
    /// </summary>
    public string Subject { get; set; } = "";

    /// <summary>
    /// Gets or sets the keywords associated with the PDF document.
    /// </summary>
    public string Keywords { get; set; } = "";

    /// <summary>
    /// Gets or sets the creator of the PDF document.
    /// </summary>
    public string Creator { get; set; } = "";

    /// <summary>
    /// Gets or sets the producer of the PDF document.
    /// </summary>
    public string Producer { get; set; } = "";
    /// <summary>
    /// Gets or sets the page count of the PDF document.
    /// </summary>
    public int PageCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the creation date and time of the PDF document.
    /// </summary>
    public DateTime CreationDateTime { get; set; } = new DateTime();

    /// <summary>
    /// Gets or sets the modification date and time of the PDF document.
    /// </summary>
    public DateTime ModificationDateTime { get; set; } = new DateTime();
}