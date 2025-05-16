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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Data.Converters;
using AyanamisTower.StellaLearning.Data;

namespace AyanamisTower.StellaLearning.Converters
{
    /// <summary>
    /// Converts a DateTimeOffset (expected to be UTC) to a formatted string
    /// representing the local date and time.
    /// </summary>
    public class DateTimeOffsetToLocalTimeStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts the UTC DateTimeOffset value to a local time string.
        /// </summary>
        /// <param name="value">The DateTimeOffset value to convert (expected UTC).</param>
        /// <param name="targetType">The type of the binding target property (should be string).</param>
        /// <param name="parameter">An optional parameter (e.g., a custom format string). If null or empty, uses a default format.</param>
        /// <param name="culture">The culture to use in the converter (usually ignored for standard formats).</param>
        /// <returns>A formatted string representing the local date and time, or an empty string/default value if conversion fails.</returns>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            // Check if the input value is a DateTimeOffset
            if (value is DateTimeOffset dto)
            {
                try
                {
                    // Convert the DateTimeOffset to the system's local time
                    DateTimeOffset localTime = dto.ToLocalTime();

                    // Determine the format string to use
                    string format = parameter as string ?? "g"; // Use parameter as format, default to "g" (short date/time) if null/empty
                    // Example: You could pass "dd/MM/yyyy HH:mm:ss" as the parameter if needed

                    // Return the formatted local time string
                    return localTime.ToString(format, culture);
                }
                catch (Exception ex)
                {
                    // Log the exception if you have logging set up
                    Console.WriteLine(
                        $"Error converting DateTimeOffset to local string: {ex.Message}"
                    ); // Basic error logging
                    return "Error"; // Return an error indicator
                }
            }

            // If the value is null or not a DateTimeOffset, return a default value or indicate inaction
            return string.Empty; // Or return AvaloniaProperty.UnsetValue or BindingOperations.DoNothing depending on desired behavior
        }

        /// <summary>
        /// Converts a string back to a DateTimeOffset (not typically needed for display).
        /// </summary>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            // ConvertBack is usually not implemented for one-way display formatting
            throw new NotSupportedException(
                "Cannot convert string back to DateTimeOffset in this converter."
            );
            // Or return BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Used to correctly deserialize and serialize space repetition items
    /// </summary>
    public class SpacedRepetitionItemConverter : JsonConverter<SpacedRepetitionItem>
    {
        private enum TypeDiscriminator
        {
            SpacedRepetitionItem = 0,
            SpacedRepetitionQuiz = 1,
            SpacedRepetitionCloze = 2,
            SpacedRepetitionFlashcard = 3,
            SpacedRepetitionVideo = 4,
            SpacedRepetitionFile = 5,
            SpacedRepetitionExercise = 6,
            SpacedRepetitionImageCloze = 7,
        }

        /// <summary>
        /// Can Convert
        /// </summary>
        /// <param name="typeToConvert"></param>
        /// <returns></returns>
        public override bool CanConvert(Type typeToConvert) =>
            typeof(SpacedRepetitionItem).IsAssignableFrom(typeToConvert);

        /// <summary>
        /// Read
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="JsonException"></exception>
        public override SpacedRepetitionItem Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            if (
                !reader.Read()
                || reader.TokenType != JsonTokenType.PropertyName
                || reader.GetString() != "TypeDiscriminator"
            )
            {
                throw new JsonException();
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }

            TypeDiscriminator typeDiscriminator = (TypeDiscriminator)reader.GetInt32();

            // Create an options object that doesnt include the converter itself
            JsonSerializerOptions optionsWithoutConverter = new(options);
            optionsWithoutConverter.Converters.Remove(this);

            SpacedRepetitionItem item = typeDiscriminator switch
            {
                TypeDiscriminator.SpacedRepetitionItem => new SpacedRepetitionItem(),
                TypeDiscriminator.SpacedRepetitionQuiz => new SpacedRepetitionQuiz(),
                TypeDiscriminator.SpacedRepetitionCloze => new SpacedRepetitionCloze(),
                TypeDiscriminator.SpacedRepetitionFlashcard => new SpacedRepetitionFlashcard(),
                TypeDiscriminator.SpacedRepetitionVideo => new SpacedRepetitionVideo(),
                TypeDiscriminator.SpacedRepetitionFile => new SpacedRepetitionFile(),
                TypeDiscriminator.SpacedRepetitionExercise => new SpacedRepetitionExercise(),
                TypeDiscriminator.SpacedRepetitionImageCloze => new SpacedRepetitionImageCloze(),
                _ => throw new JsonException("Type Discriminator not found"),
            };

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return item;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString()!;
                    reader.Read();
                    switch (item)
                    {
                        case SpacedRepetitionQuiz quiz
                            when propertyName == nameof(SpacedRepetitionQuiz.Question):
                            quiz.Question = reader.GetString()!;
                            break;
                        case SpacedRepetitionQuiz quiz
                            when propertyName == nameof(SpacedRepetitionQuiz.Answers):
                            quiz.Answers = JsonSerializer.Deserialize<List<string>>(
                                ref reader,
                                optionsWithoutConverter
                            )!;
                            break;
                        case SpacedRepetitionQuiz quiz
                            when propertyName == nameof(SpacedRepetitionQuiz.CorrectAnswerIndex):
                            quiz.CorrectAnswerIndex = reader.GetInt32();
                            break;
                        case SpacedRepetitionCloze cloze
                            when propertyName == nameof(SpacedRepetitionCloze.FullText):
                            cloze.FullText = reader.GetString()!;
                            break;
                        case SpacedRepetitionCloze cloze
                            when propertyName == nameof(SpacedRepetitionCloze.ClozeWords):
                            cloze.ClozeWords = JsonSerializer.Deserialize<List<string>>(
                                ref reader,
                                optionsWithoutConverter
                            )!;
                            break;
                        case SpacedRepetitionFlashcard flashcard
                            when propertyName == nameof(SpacedRepetitionFlashcard.Front):
                            flashcard.Front = reader.GetString()!;
                            break;
                        case SpacedRepetitionFlashcard flashcard
                            when propertyName == nameof(SpacedRepetitionFlashcard.Back):
                            flashcard.Back = reader.GetString()!;
                            break;
                        case SpacedRepetitionVideo video
                            when propertyName == nameof(SpacedRepetitionVideo.VideoUrl):
                            video.VideoUrl = reader.GetString()!;
                            break;
                        case SpacedRepetitionFile file
                            when propertyName == nameof(SpacedRepetitionFile.FilePath):
                            file.FilePath = reader.GetString()!;
                            break;
                        case SpacedRepetitionFile file
                            when propertyName == nameof(SpacedRepetitionFile.Question):
                            file.Question = reader.GetString()!;
                            break;
                        case SpacedRepetitionExercise exercise
                            when propertyName == nameof(SpacedRepetitionExercise.Problem):
                            exercise.Problem = reader.GetString()!;
                            break;
                        case SpacedRepetitionExercise exercise
                            when propertyName == nameof(SpacedRepetitionExercise.Solution):
                            exercise.Solution = reader.GetString()!;
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.Uid):
                            baseItem.Uid = Guid.Parse(reader.GetString()!);
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.Name):
                            baseItem.Name = reader.GetString()!;
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.Tags):
                            baseItem.Tags = JsonSerializer.Deserialize<List<string>>(
                                ref reader,
                                optionsWithoutConverter
                            )!;
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.Stability):
                            baseItem.Stability =
                                reader.TokenType == JsonTokenType.Null ? null : reader.GetSingle();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.Difficulty):
                            baseItem.Difficulty =
                                reader.TokenType == JsonTokenType.Null ? null : reader.GetSingle();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.Priority):
                            baseItem.Priority = reader.GetInt32();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.Step):
                            baseItem.Step =
                                reader.TokenType == JsonTokenType.Null
                                    ? null
                                    : (int)reader.GetInt64();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.LastReview):
                            baseItem.LastReview =
                                reader.TokenType == JsonTokenType.Null
                                    ? null
                                    : reader.GetDateTime();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.NextReview):
                            baseItem.NextReview = reader.GetDateTime();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.NumberOfTimesSeen):
                            baseItem.NumberOfTimesSeen = reader.GetInt32();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.ElapsedDays):
                            baseItem.ElapsedDays = reader.GetInt32();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.ScheduledDays):
                            baseItem.ScheduledDays = reader.GetInt32();
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName == nameof(SpacedRepetitionItem.SpacedRepetitionState):
                            baseItem.SpacedRepetitionState = Enum.Parse<SpacedRepetitionState>(
                                reader.GetString()!
                            );
                            break;
                        case SpacedRepetitionItem baseItem
                            when propertyName
                                == nameof(SpacedRepetitionItem.SpacedRepetitionItemType):
                            baseItem.SpacedRepetitionItemType =
                                Enum.Parse<SpacedRepetitionItemType>(reader.GetString()!);
                            break;
                        case SpacedRepetitionImageCloze imageCloze
                            when propertyName == nameof(SpacedRepetitionImageCloze.ImagePath):
                            imageCloze.ImagePath = reader.GetString()!;
                            break;
                        case SpacedRepetitionImageCloze imageCloze
                            when propertyName == nameof(SpacedRepetitionImageCloze.ClozeAreas):
                            imageCloze.ClozeAreas = JsonSerializer.Deserialize<
                                List<ImageClozeArea>
                            >(ref reader, optionsWithoutConverter)!;
                            break;
                        default:
                            reader.Skip(); // Skip unknown properties
                            break;
                    }
                }
            }
            throw new JsonException("Failed to read all properties of SpacedRepetitionItem");
        }

        /// <summary>
        /// Custom Write
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="item"></param>
        /// <param name="options"></param>
        public override void Write(
            Utf8JsonWriter writer,
            SpacedRepetitionItem item,
            JsonSerializerOptions options
        )
        {
            // Create an options object that doesnt include the converter itself
            JsonSerializerOptions optionsWithoutConverter = new(options);
            optionsWithoutConverter.Converters.Remove(this);

            writer.WriteStartObject();

            switch (item)
            {
                case SpacedRepetitionQuiz quiz:
                    writer.WriteNumber(
                        "TypeDiscriminator",
                        (int)TypeDiscriminator.SpacedRepetitionQuiz
                    );
                    writer.WriteString(nameof(SpacedRepetitionQuiz.Question), quiz.Question);
                    writer.WritePropertyName(nameof(SpacedRepetitionQuiz.Answers));
                    JsonSerializer.Serialize(writer, quiz.Answers, optionsWithoutConverter);
                    writer.WriteNumber(
                        nameof(SpacedRepetitionQuiz.CorrectAnswerIndex),
                        quiz.CorrectAnswerIndex
                    );
                    break;
                case SpacedRepetitionCloze cloze:
                    writer.WriteNumber(
                        "TypeDiscriminator",
                        (int)TypeDiscriminator.SpacedRepetitionCloze
                    );
                    writer.WriteString(nameof(SpacedRepetitionCloze.FullText), cloze.FullText);
                    writer.WritePropertyName(nameof(SpacedRepetitionCloze.ClozeWords));
                    JsonSerializer.Serialize(writer, cloze.ClozeWords, optionsWithoutConverter);
                    break;
                case SpacedRepetitionFlashcard flashcard:
                    writer.WriteNumber(
                        "TypeDiscriminator",
                        (int)TypeDiscriminator.SpacedRepetitionFlashcard
                    );
                    writer.WriteString(nameof(SpacedRepetitionFlashcard.Front), flashcard.Front);
                    writer.WriteString(nameof(SpacedRepetitionFlashcard.Back), flashcard.Back);
                    break;
                case SpacedRepetitionVideo video:
                    writer.WriteNumber(
                        "TypeDiscriminator",
                        (int)TypeDiscriminator.SpacedRepetitionVideo
                    );
                    writer.WriteString(nameof(SpacedRepetitionVideo.VideoUrl), video.VideoUrl);
                    break;
                case SpacedRepetitionFile file:
                    writer.WriteNumber(
                        "TypeDiscriminator",
                        (int)TypeDiscriminator.SpacedRepetitionFile
                    );
                    writer.WriteString(nameof(SpacedRepetitionFile.Question), file.Question);
                    writer.WriteString(nameof(SpacedRepetitionFile.FilePath), file.FilePath);
                    break;
                case SpacedRepetitionExercise exercise:
                    writer.WriteNumber(
                        "TypeDiscriminator",
                        (int)TypeDiscriminator.SpacedRepetitionExercise
                    );
                    writer.WriteString(nameof(SpacedRepetitionExercise.Problem), exercise.Problem);
                    writer.WriteString(
                        nameof(SpacedRepetitionExercise.Solution),
                        exercise.Solution
                    );
                    break;
                case SpacedRepetitionImageCloze imageCloze:
                    writer.WriteNumber(
                        "TypeDiscriminator",
                        (int)TypeDiscriminator.SpacedRepetitionImageCloze
                    );
                    writer.WriteString(
                        nameof(SpacedRepetitionImageCloze.ImagePath),
                        imageCloze.ImagePath
                    );
                    writer.WritePropertyName(nameof(SpacedRepetitionImageCloze.ClozeAreas));
                    JsonSerializer.Serialize(
                        writer,
                        imageCloze.ClozeAreas,
                        optionsWithoutConverter
                    );
                    break;
                default:
                    writer.WriteNumber(
                        "TypeDiscriminator",
                        (int)TypeDiscriminator.SpacedRepetitionItem
                    );
                    break;
            }

            // Write common properties for all SpacedRepetitionItems
            writer.WriteString(nameof(SpacedRepetitionItem.Uid), item.Uid.ToString());
            writer.WriteString(nameof(SpacedRepetitionItem.Name), item.Name);
            writer.WritePropertyName(nameof(SpacedRepetitionItem.Tags));
            JsonSerializer.Serialize(writer, item.Tags, optionsWithoutConverter);
            if (item.Stability.HasValue)
                writer.WriteNumber(nameof(SpacedRepetitionItem.Stability), item.Stability.Value);
            else
                writer.WriteNull(nameof(SpacedRepetitionItem.Stability));
            if (item.Difficulty.HasValue)
                writer.WriteNumber(nameof(SpacedRepetitionItem.Difficulty), item.Difficulty.Value);
            else
                writer.WriteNull(nameof(SpacedRepetitionItem.Difficulty));
            writer.WriteNumber(nameof(SpacedRepetitionItem.Priority), item.Priority);
            if (item.Step.HasValue)
                writer.WriteNumber(nameof(SpacedRepetitionItem.Step), item.Step.Value);
            else
                writer.WriteNull(nameof(SpacedRepetitionItem.Step));
            if (item.LastReview.HasValue)
                writer.WriteString(
                    nameof(SpacedRepetitionItem.LastReview),
                    item.LastReview.Value.ToString("o")
                );
            else
                writer.WriteNull(nameof(SpacedRepetitionItem.LastReview));
            writer.WriteString(
                nameof(SpacedRepetitionItem.NextReview),
                item.NextReview.ToString("o")
            );
            writer.WriteNumber(
                nameof(SpacedRepetitionItem.NumberOfTimesSeen),
                item.NumberOfTimesSeen
            );
            writer.WriteNumber(nameof(SpacedRepetitionItem.ElapsedDays), item.ElapsedDays);
            writer.WriteNumber(nameof(SpacedRepetitionItem.ScheduledDays), item.ScheduledDays);
            writer.WriteString(
                nameof(SpacedRepetitionItem.SpacedRepetitionState),
                item.SpacedRepetitionState.ToString()
            );
            writer.WriteString(
                nameof(SpacedRepetitionItem.SpacedRepetitionItemType),
                item.SpacedRepetitionItemType.ToString()
            );

            writer.WriteEndObject();
        }
    }
}
