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
                               .Select(fd => fd.Attribute("Key")?.Value)
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
    /// Reads the galaxy data from an XML file where galaxy names are attributes,
    /// and returns the names of the galaxies.
    /// </summary>
    /// <param name="relativeFilePath">The relative path to the XML file.</param>
    /// <returns>A list of galaxy names. Returns an empty list if the file is not found or an error occurs.</returns>
    public static List<string?> GetGalaxyNames(string relativeFilePath)
    {
        List<string?> galaxyNames = [];
        try
        {
            // Get the base directory of the application
            // For .NET Core/5/6/7/8+
            string baseDirectory = AppContext.BaseDirectory;
            // Alternatively, for .NET Framework (uncomment if needed):
            // string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Combine the base directory with the relative file path
            string fullPath = Path.Combine(baseDirectory, relativeFilePath);

            // Check if the file exists before attempting to load
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Error: XML file not found at {fullPath}");
                // Return empty list if file not found
                return [];
            }

            // Load the XML document from the file
            XDocument doc = XDocument.Load(fullPath);

            // Query the document to find all "Galaxy" elements
            // and then select the value of their "Name" attribute.
            // The key change is g.Attribute("Name") instead of g.Element("Name")
            galaxyNames = doc.Descendants("Galaxy")       // Find all <Galaxy> elements
                             .Select(g => g.Attribute("Name")?.Value) // Get the value of the "Name" attribute
                             .Where(name => !string.IsNullOrEmpty(name)) // Filter out any null or empty names
                             .ToList(); // Convert to List

            // If you want to ensure the list doesn't contain nulls and make its type List<string>
            // you could do this, but List<string?> is fine if attributes might be missing.
            // List<string> galaxyNamesNonNull = doc.Descendants("Galaxy")
            //                                 .Select(g => g.Attribute("Name")?.Value)
            //                                 .Where(name => !string.IsNullOrEmpty(name))
            //                                 .Select(name => name!) // Assert name is not null here
            //                                 .ToList();
            // return galaxyNamesNonNull;

        }
        catch (System.Xml.XmlException ex)
        {
            Console.WriteLine($"Error parsing XML file '{relativeFilePath}': {ex.Message}");
            // Handle XML format errors, or rethrow if critical
            // Depending on requirements, you might want to return an empty list or throw
        }
        catch (IOException ex)
        {
            Console.WriteLine($"An I/O error occurred while accessing file '{relativeFilePath}': {ex.Message}");
        }
        catch (Exception ex) // Catch other potential exceptions
        {
            Console.WriteLine($"An unexpected error occurred while processing file '{relativeFilePath}': {ex.Message}");
            // Handle other errors, or rethrow if critical
        }
        return galaxyNames;
    }
}
