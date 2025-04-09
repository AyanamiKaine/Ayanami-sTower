using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes; // Required for JsonNode

// Make sure this namespace matches where your LiteratureSourceItem and derived classes are
namespace Avalonia.Flecs.StellaLearning.Data
{
    /// <summary>
    /// Handles polymorphic serialization and deserialization for LiteratureSourceItem and its derived types.
    /// Uses the 'SourceType' property as a discriminator to determine the concrete type.
    /// </summary>
    public class LiteratureSourceItemConverter : JsonConverter<LiteratureSourceItem>
    {
        // Define the JSON property name for the discriminator.
        // Respecting potential naming policies (like camelCase) is important.
        // Let's assume camelCase for now, which is the default for System.Text.Json.
        // If you configure a different policy, adjust this string accordingly.
        private const string DiscriminatorPropertyName = "SourceType"; // Adjust if your naming policy differs

        /// <summary>
        /// Determines if this converter can handle the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to check.</param>
        /// <returns>True if the type is LiteratureSourceItem or derives from it; otherwise, false.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            // This converter handles the base class and any class inheriting from it.
            return typeof(LiteratureSourceItem).IsAssignableFrom(typeToConvert);
        }
        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="typeToConvert">The type expected (will be LiteratureSourceItem).</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The deserialized LiteratureSourceItem (as one of its derived types).</returns>
        public override LiteratureSourceItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Check for null JSON token
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // --- Prevent StackOverflow during nested deserialization ---
            // Create a new options instance based on the current ones,
            // but *without* this specific converter instance. This will be used
            // for the internal Deserialize call later.
            var innerOptions = new JsonSerializerOptions(options);
            JsonConverter? selfConverter = null;
            foreach (var converter in innerOptions.Converters)
            {
                if (converter.GetType() == this.GetType())
                {
                    selfConverter = converter;
                    break;
                }
            }
            if (selfConverter != null)
            {
                innerOptions.Converters.Remove(selfConverter);
            }
            else
            {
                // Log or handle the unlikely case where the converter isn't found
                Console.Error.WriteLine($"Warning: {nameof(LiteratureSourceItemConverter)} could not find itself in the options during Read setup. Deserialization might proceed normally, but this is unexpected.");
                // Consider throwing if this state is invalid:
                // throw new InvalidOperationException($"{nameof(LiteratureSourceItemConverter)} could not find itself in the options during Read operation.");
            }
            // --- End StackOverflow Prevention Setup ---


            // Use JsonNode to parse the object.
            // The StackOverflow *might* appear here if the JSON is excessively deep
            // OR if JsonNode.Parse itself triggers internal converter calls unexpectedly,
            // but often the true recursion happens in the subsequent Deserialize call.
            JsonNode? node = JsonNode.Parse(ref reader); // Parse using the ORIGINAL reader/options implicitly
            if (node == null)
            {
                return null; // Handle cases where parsing results in null
            }


            // Determine the Concrete Type using the Discriminator
            if (!node.AsObject().TryGetPropertyValue(DiscriminatorPropertyName, out JsonNode? discriminatorNode) || discriminatorNode == null)
            {
                throw new JsonException($"Required discriminator property '{DiscriminatorPropertyName}' not found or is null in JSON: {node.ToJsonString()}");
            }

            LiteratureSourceType sourceType;
            try
            {
                JsonElement discriminatorElement = discriminatorNode.GetValue<JsonElement>();
                if (discriminatorElement.ValueKind == JsonValueKind.String)
                {
                    if (!Enum.TryParse<LiteratureSourceType>(discriminatorElement.GetString(), true, out sourceType))
                    {
                        throw new JsonException($"Invalid string value '{discriminatorElement.GetString()}' for discriminator '{DiscriminatorPropertyName}'.");
                    }
                }
                else if (discriminatorElement.ValueKind == JsonValueKind.Number)
                {
                    if (!discriminatorElement.TryGetInt32(out int enumIntValue) || !Enum.IsDefined(typeof(LiteratureSourceType), enumIntValue))
                    {
                        throw new JsonException($"Invalid integer value for discriminator '{DiscriminatorPropertyName}'. Value: {discriminatorElement.GetRawText()}");
                    }
                    sourceType = (LiteratureSourceType)enumIntValue;
                }
                else
                {
                    throw new JsonException($"Discriminator '{DiscriminatorPropertyName}' must be a string or number, but was {discriminatorElement.ValueKind}.");
                }
            }
            catch (Exception ex) when (ex is not JsonException)
            {
                throw new JsonException($"Error parsing discriminator property '{DiscriminatorPropertyName}'. See inner exception.", ex);
            }

            // Deserialize to the Specific Derived Type using the INNER options
            // This is the call that most likely causes recursion if using original 'options'
            LiteratureSourceItem? result = sourceType switch
            {
                // *** Use innerOptions here ***
                LiteratureSourceType.LocalFile => node.Deserialize<LocalFileSourceItem>(innerOptions),
                LiteratureSourceType.Website => node.Deserialize<WebSourceItem>(innerOptions),

                // *** Add cases for Book, JournalArticle, etc. using innerOptions ***
                // LiteratureSourceType.Book => node.Deserialize<BookSourceItem>(innerOptions),

                _ => throw new JsonException($"Unsupported LiteratureSourceType '{sourceType}' encountered during deserialization.")
            };

            return result;
        }


        /// <summary>
        /// Writes the JSON representation of the object, handling potential recursion.
        /// </summary>
        public override void Write(Utf8JsonWriter writer, LiteratureSourceItem value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // --- Prevent StackOverflow ---
            // Create a new options instance based on the current ones,
            // but *without* this specific converter instance to avoid recursion.
            var innerOptions = new JsonSerializerOptions(options);

            // Find and remove this converter instance from the temporary options.
            // We search by type; assuming only one instance of this converter is registered.
            JsonConverter? selfConverter = null;
            foreach (var converter in innerOptions.Converters)
            {
                // Check if the converter in the list is of the same type as this instance.
                if (converter.GetType() == this.GetType())
                {
                    selfConverter = converter;
                    break;
                }
            }

            if (selfConverter != null)
            {
                innerOptions.Converters.Remove(selfConverter);
            }
            else
            {
                // This case should ideally not happen if the Write method was called through this converter,
                // but adding a safeguard or log might be useful depending on the scenario.
                Console.Error.WriteLine($"Warning: {nameof(LiteratureSourceItemConverter)} could not find itself in the options. Serialization might proceed normally, but this is unexpected.");
                // Or potentially throw an exception if this state is considered invalid:
                // throw new InvalidOperationException($"{nameof(LiteratureSourceItemConverter)} could not find itself in the options during Write operation.");
            }


            // Now, serialize using the modified 'innerOptions' that lack this converter.
            // This forces System.Text.Json to use its default serialization logic for the
            // actual type (value.GetType()), writing all its properties without calling this Write method again.
            JsonSerializer.Serialize(writer, value, value.GetType(), innerOptions);
        }
    }
}