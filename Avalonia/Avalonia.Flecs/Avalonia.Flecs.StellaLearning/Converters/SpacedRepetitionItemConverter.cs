using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Flecs.StellaLearning.Data;

namespace Avalonia.Flecs.StellaLearning.Converters
{
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
            SpacedRepetitionExercise = 6
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
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != "TypeDiscriminator")
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
                _ => throw new JsonException("Type Discriminator not found")
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
                        case SpacedRepetitionQuiz quiz when propertyName == nameof(SpacedRepetitionQuiz.Question):
                            quiz.Question = reader.GetString()!;
                            break;
                        case SpacedRepetitionQuiz quiz when propertyName == nameof(SpacedRepetitionQuiz.Answers):
                            quiz.Answers = JsonSerializer.Deserialize<List<string>>(ref reader, optionsWithoutConverter)!;
                            break;
                        case SpacedRepetitionQuiz quiz when propertyName == nameof(SpacedRepetitionQuiz.CorrectAnswerIndex):
                            quiz.CorrectAnswerIndex = reader.GetInt32();
                            break;
                        case SpacedRepetitionCloze cloze when propertyName == nameof(SpacedRepetitionCloze.FullText):
                            cloze.FullText = reader.GetString()!;
                            break;
                        case SpacedRepetitionCloze cloze when propertyName == nameof(SpacedRepetitionCloze.ClozeWords):
                            cloze.ClozeWords = JsonSerializer.Deserialize<List<string>>(ref reader, optionsWithoutConverter)!;
                            break;
                        case SpacedRepetitionFlashcard flashcard when propertyName == nameof(SpacedRepetitionFlashcard.Front):
                            flashcard.Front = reader.GetString()!;
                            break;
                        case SpacedRepetitionFlashcard flashcard when propertyName == nameof(SpacedRepetitionFlashcard.Back):
                            flashcard.Back = reader.GetString()!;
                            break;
                        case SpacedRepetitionVideo video when propertyName == nameof(SpacedRepetitionVideo.VideoUrl):
                            video.VideoUrl = reader.GetString()!;
                            break;
                        case SpacedRepetitionFile file when propertyName == nameof(SpacedRepetitionFile.FilePath):
                            file.FilePath = reader.GetString()!;
                            break;
                        case SpacedRepetitionExercise exercise when propertyName == nameof(SpacedRepetitionExercise.Problem):
                            exercise.Problem = reader.GetString()!;
                            break;
                        case SpacedRepetitionExercise exercise when propertyName == nameof(SpacedRepetitionExercise.Solution):
                            exercise.Solution = reader.GetString()!;
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.Uid):
                            baseItem.Uid = Guid.Parse(reader.GetString()!);
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.Name):
                            baseItem.Name = reader.GetString()!;
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.Tags):
                            baseItem.Tags = JsonSerializer.Deserialize<List<string>>(ref reader, optionsWithoutConverter)!;
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.Stability):
                            baseItem.Stability = reader.TokenType == JsonTokenType.Null ? null : reader.GetSingle();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.Difficulty):
                            baseItem.Difficulty = reader.TokenType == JsonTokenType.Null ? null : reader.GetSingle();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.Priority):
                            baseItem.Priority = reader.GetInt32();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.Step):
                            baseItem.Step = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt64();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.LastReview):
                            baseItem.LastReview = reader.TokenType == JsonTokenType.Null ? null : reader.GetDateTime();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.NextReview):
                            baseItem.NextReview = reader.GetDateTime();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.NumberOfTimesSeen):
                            baseItem.NumberOfTimesSeen = reader.GetInt32();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.ElapsedDays):
                            baseItem.ElapsedDays = reader.GetInt32();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.ScheduledDays):
                            baseItem.ScheduledDays = reader.GetInt32();
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.SpacedRepetitionState):
                            baseItem.SpacedRepetitionState = Enum.Parse<SpacedRepetitionState>(reader.GetString()!);
                            break;
                        case SpacedRepetitionItem baseItem when propertyName == nameof(SpacedRepetitionItem.SpacedRepetitionItemType):
                            baseItem.SpacedRepetitionItemType = Enum.Parse<SpacedRepetitionItemType>(reader.GetString()!);
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
            Utf8JsonWriter writer, SpacedRepetitionItem item, JsonSerializerOptions options)
        {
            // Create an options object that doesnt include the converter itself
            JsonSerializerOptions optionsWithoutConverter = new(options);
            optionsWithoutConverter.Converters.Remove(this);

            writer.WriteStartObject();

            switch (item)
            {
                case SpacedRepetitionQuiz quiz:
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.SpacedRepetitionQuiz);
                    writer.WriteString(nameof(SpacedRepetitionQuiz.Question), quiz.Question);
                    writer.WritePropertyName(nameof(SpacedRepetitionQuiz.Answers));
                    JsonSerializer.Serialize(writer, quiz.Answers, optionsWithoutConverter);
                    writer.WriteNumber(nameof(SpacedRepetitionQuiz.CorrectAnswerIndex), quiz.CorrectAnswerIndex);
                    break;
                case SpacedRepetitionCloze cloze:
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.SpacedRepetitionCloze);
                    writer.WriteString(nameof(SpacedRepetitionCloze.FullText), cloze.FullText);
                    writer.WritePropertyName(nameof(SpacedRepetitionCloze.ClozeWords));
                    JsonSerializer.Serialize(writer, cloze.ClozeWords, optionsWithoutConverter);
                    break;
                case SpacedRepetitionFlashcard flashcard:
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.SpacedRepetitionFlashcard);
                    writer.WriteString(nameof(SpacedRepetitionFlashcard.Front), flashcard.Front);
                    writer.WriteString(nameof(SpacedRepetitionFlashcard.Back), flashcard.Back);
                    break;
                case SpacedRepetitionVideo video:
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.SpacedRepetitionVideo);
                    writer.WriteString(nameof(SpacedRepetitionVideo.VideoUrl), video.VideoUrl);
                    break;
                case SpacedRepetitionFile file:
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.SpacedRepetitionFile);
                    writer.WriteString(nameof(SpacedRepetitionFile.FilePath), file.FilePath);
                    break;
                case SpacedRepetitionExercise exercise:
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.SpacedRepetitionExercise);
                    writer.WriteString(nameof(SpacedRepetitionExercise.Problem), exercise.Problem);
                    writer.WriteString(nameof(SpacedRepetitionExercise.Solution), exercise.Solution);
                    break;
                default:
                    writer.WriteNumber("TypeDiscriminator", (int)TypeDiscriminator.SpacedRepetitionItem);
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
                writer.WriteString(nameof(SpacedRepetitionItem.LastReview), item.LastReview.Value.ToString("o"));
            else
                writer.WriteNull(nameof(SpacedRepetitionItem.LastReview));
            writer.WriteString(nameof(SpacedRepetitionItem.NextReview), item.NextReview.ToString("o"));
            writer.WriteNumber(nameof(SpacedRepetitionItem.NumberOfTimesSeen), item.NumberOfTimesSeen);
            writer.WriteNumber(nameof(SpacedRepetitionItem.ElapsedDays), item.ElapsedDays);
            writer.WriteNumber(nameof(SpacedRepetitionItem.ScheduledDays), item.ScheduledDays);
            writer.WriteString(nameof(SpacedRepetitionItem.SpacedRepetitionState), item.SpacedRepetitionState.ToString());
            writer.WriteString(nameof(SpacedRepetitionItem.SpacedRepetitionItemType), item.SpacedRepetitionItemType.ToString());

            writer.WriteEndObject();
        }
    }
}