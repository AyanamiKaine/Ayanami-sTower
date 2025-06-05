using System;
using System.Xml.Linq;

namespace AyanamisTower.StellaDB;

/// <summary>
/// Parses the data found.
/// </summary>
public static class DataParser
{
    /// <summary>
    /// Gets all feature keys defined
    /// </summary>
    /// <param name="relativeFilePath"></param>
    /// <returns></returns>
    public static List<string?> GetFeatureKeys(string relativeFilePath)
    {
        List<string?> featureKeys = [];
        try
        {
            // Get the base directory of the application
            string baseDirectory = AppContext.BaseDirectory;
            // Alternatively, for .NET Framework: string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Combine the base directory with the relative file path
            string fullPath = Path.Combine(baseDirectory, relativeFilePath);

            // Check if the file exists before attempting to load
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Error: XML file not found at {fullPath}");
                // You might want to throw an exception here or return an empty list,
                // depending on how you want to handle missing files.
                return featureKeys; // Return empty list
            }

            // Load the XML document from the file
            XDocument doc = XDocument.Load(fullPath);

            // Query the document to find all "FeatureDefinition" elements
            // and then select the value of their "Key" child element.
            featureKeys = [.. doc.Descendants("FeatureDefinition")
                               .Select(fd => fd.Element("Key")?.Value)
                               .Where(key => !string.IsNullOrEmpty(key))];
        }
        catch (System.Xml.XmlException ex)
        {
            Console.WriteLine($"Error: XML parsing error. {ex.Message}");
            // Handle XML format errors
        }
        catch (Exception ex) // Catch-all for other unexpected errors
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }

        return featureKeys;
    }

    /// <summary>
    /// Reads the galaxy data, and returns the names of the galaxies
    /// </summary>
    /// <param name="relativeFilePath"></param>
    /// <returns></returns>
    public static List<string?> GetGalaxyNames(string relativeFilePath)
    {
        List<string?> galaxyNames = [];
        try
        {
            // Get the base directory of the application
            string baseDirectory = AppContext.BaseDirectory;
            // Alternatively, for .NET Framework: string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Combine the base directory with the relative file path
            string fullPath = Path.Combine(baseDirectory, relativeFilePath);

            // Check if the file exists before attempting to load
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Error: XML file not found at {fullPath}");
                // You might want to throw an exception here or return an empty list,
                // depending on how you want to handle missing files.
                return []; // Return empty list
            }

            // Load the XML document from the file
            XDocument doc = XDocument.Load(fullPath);

            // Query the document to find all "Galaxy" elements
            // and then select the value of their "Name" child element.
            galaxyNames = [.. doc.Descendants("Galaxy")    // Find all <Galaxy> elements
                             .Select(g => g.Element("Name")?.Value) // Get the text of the <Name> child
                             .Where(name => !string.IsNullOrEmpty(name))];
        }
        catch (System.Xml.XmlException ex)
        {
            Console.WriteLine($"Error parsing XML string: {ex.Message}");
            // Handle XML format errors, or rethrow if critical
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
        return galaxyNames;
    }
}
