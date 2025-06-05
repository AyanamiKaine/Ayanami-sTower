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
            string baseDirectory = AppContext.BaseDirectory;

            string fullPath = Path.Combine(baseDirectory, relativeFilePath);

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Error: XML file not found at {fullPath}");
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


    /*
    TODO:
    There must be a way to easily add components to data. We need a generic parse method that can infere based on 
    the structure what tables and attributes should be used. 

    <StarSystem Name = "Sirius">
        <Galaxy Name = "Milky Way"/>
        <Components>
            <Position X="-60" Y="-20" Z="-20"/>
        </Components>
    </StarSystem>

    Here we should understand that every element in components is another table and its attributes are columns in it.

    Maybe every element is a another table associated with the entity. And the attributes for an element are the columns so 
    we need to rewrite the xml structure like so:
    <StarSystem Name = "Sirius", Galaxy = "Milky Way">
        <Position X="-60" Y="-20" Z="-20"/>
    </StarSystem>

    But then again its not really correct. Because correct would be saying parent. And name is not a column in entities but instead
    another component.

    <StarSystem Parent = "Milky Way">
        <Name>Sirius</Name>
        <Position X="-60" Y="-20" Z="-20"/>
    </StarSystem>

    Maybe being more explicit how entities are constructed. Its much more symetric, because this reflect the structure
    we create in code, and in the database. This works because the id of entities can be queried using their unique name
    so we dont have to write Parent=2231 should the id change for any reason we would need to change it here too.

    <Entity Parent="Milky Way">
        <StarSystem/>
        <Name Value="Sirius"/>
        <Position X="-60" Y="-20" Z="-20"/>
    </Entity>

    */

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
            string baseDirectory = AppContext.BaseDirectory;

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

            galaxyNames = [.. doc.Descendants("Galaxy")       // Find all <Galaxy> elements
                             .Select(g => g.Attribute("Name")?.Value) // Get the value of the "Name" attribute
                             .Where(name => !string.IsNullOrEmpty(name))]; // Convert to List


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
