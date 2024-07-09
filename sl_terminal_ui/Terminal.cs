using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using static NetMQ.NetMQSelector;

namespace Terminal_UI_Component
{
    /// <summary>
    /// Represents the Terminal UI for interacting with SKM
    /// </summary>
    public class Terminal
    {

        RequestSocket SpaceRepetitionDatabaseClient = new RequestSocket(">tcp://localhost:60002"); // Clients always start with an >
        RequestSocket SpaceRepetitionAlgorithmClient = new RequestSocket(">tcp://localhost:60005");
        DealerSocket OpenFileWithDefaultProgramClient = new DealerSocket(">tcp://localhost:60001");
        DealerSocket LoggerClient = new DealerSocket(">tcp://localhost:60010");    // Clients always start with an >

        public void LearnItem(FileToLearn item)
        {
            Console.WriteLine($"Start Learning the item {item.Name}");
            Console.WriteLine($"Description: '{item.Description}'");
            Console.WriteLine($"Press Enter To Open The File {item.PathToFile} to Continue...");
            Console.ReadLine().ToLower();

            string serlized_item = JsonSerializer.Serialize(item);

            OpenFileWithDefaultProgramClient.SendFrame(item.PathToFile);

            Console.WriteLine("Now Give Yourself feedback how well did you achieve your goal?");
            Console.WriteLine("Enter 'g' for good, 'b' for bad, or 'r' to do it again:");
        }

        public void PriorityLearning()
        {

            SpaceRepetitionDatabaseClient.SendFrame("""
                            {
                                "Command": "RETRIVE_ALL_ITEMS"
                            }
                            """);

            string all_items_string = SpaceRepetitionDatabaseClient.ReceiveFrameString();
            List<FileToLearn> PriorityList = JsonSerializer.Deserialize<List<FileToLearn>>(all_items_string);

            foreach (var item in PriorityList)
            {

                bool itemIsDue = true;

                if (item.NextReviewDate >= DateTime.Now)
                {
                    itemIsDue = false;
                }

                while (itemIsDue)
                {
                    try
                    {
                        LearnItem(item);

                        bool validInput = false;

                        while (!validInput)
                        {
                            string input = Console.ReadLine().ToLower(); // Read input and convert to lowercase

                            if (input == "g")
                            {
                                Console.WriteLine("Great! Let's move on.");
                                validInput = true;

                                string file_to_learn_serialization = JsonSerializer.Serialize(item);

                                string json_request = $"{{ \"RecallEvaluation\": \"Good\", \"FileToLearn\": {file_to_learn_serialization} }}";
                                SpaceRepetitionAlgorithmClient.SendFrame(json_request);
                                string json_response = SpaceRepetitionAlgorithmClient.ReceiveFrameString();
                                DateTime new_due_date = JsonSerializer.Deserialize<DateTime>(json_response);
                                item.NextReviewDate = new_due_date;
                            }
                            else if (input == "b")
                            {
                                Console.WriteLine("Don't worry, we can try again later.");
                                validInput = true;
                                string file_to_learn_serialization = JsonSerializer.Serialize(item);

                                string json_request = $"{{ \"RecallEvaluation\": \"Bad\", \"FileToLearn\": {file_to_learn_serialization} }}";
                                SpaceRepetitionAlgorithmClient.SendFrame(json_request);
                                string json_response = SpaceRepetitionAlgorithmClient.ReceiveFrameString();
                                DateTime new_due_date = JsonSerializer.Deserialize<DateTime>(json_response);
                                item.NextReviewDate = new_due_date;

                            }
                            else if (input == "r")
                            {
                                Console.WriteLine("Let's try that again.");
                                validInput = true;
                                item.NextReviewDate = DateTime.Now.AddMinutes(5);
                            }
                            else
                            {
                                Console.WriteLine("Invalid input. Please enter 'g', 'b', or 'r'.");
                                validInput = false;
                            }
                        }
                        Console.WriteLine($"New Due Date: {item.NextReviewDate}");
                        item.NumberOfTimeSeen += 1;
                        Console.WriteLine($"New Number of Times Seen: {item.NumberOfTimeSeen}");

                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Terminal Exception: ({ex.Message})");
                    }

                    if (itemIsDue)
                    {
                        Console.WriteLine($"Current Priority : {item.Priority}");
                        Console.WriteLine("Do you wish to change the priority of the current item? (Y)es/(N)o (Default NO)");
                        var changePriorityResponse = Console.ReadLine().ToLower();

                        if (changePriorityResponse == "y")
                        {
                            ChangeCurrentPriority(item);
                        }
                    }

                    if (item.NextReviewDate >= DateTime.Now)
                    {
                        itemIsDue = false;
                    }
                    UpdateItem(item);
                }
                Console.WriteLine($"Item: '{item.Name}' next review day is {item.NextReviewDate}");
            }
        }

        public static void ChangeCurrentPriority(FileToLearn fileToLearn)
        {
            double newPriority = 0;

            Console.WriteLine("Type the new priority you want to assign");
            bool parsingPrioritySuccess = double.TryParse(Console.ReadLine().ToLower(), out newPriority);

            if (parsingPrioritySuccess)
            {
                fileToLearn.Priority = newPriority;
            }
        }

        // You should be able to give an exercise and let the user give an answer and the program should calculate if the 
        // Answer was good. For example, write a abstract base class in C++, you then get an answer on how good you did.
        // This could be a good place for a LLM 

        void UpdateItem(FileToLearn fileToLearn)
        {
            SpaceRepetitionDatabaseClient.SendFrame($"{{ \"Command\": \"UPDATE\", \"FileToLearn\": {JsonSerializer.Serialize(fileToLearn)} }}");
            SpaceRepetitionDatabaseClient.ReceiveFrameString();
        }

        void AddNewFile(FileToLearn newFileToLearn)
        {
            SpaceRepetitionDatabaseClient.SendFrame($"{{ \"Command\": \"CREATE\", \"FileToLearn\": {JsonSerializer.Serialize(newFileToLearn)} }}");
            SpaceRepetitionDatabaseClient.ReceiveFrameString();
        }

        void DeleteItem(Guid guideId)
        {
            SpaceRepetitionDatabaseClient.SendFrame($"{{ \"Command\": \"DELETE\", \"Id\": {JsonSerializer.Serialize(guideId)} }}");
            SpaceRepetitionDatabaseClient.ReceiveFrameString();
        }

        /// <summary>
        /// Starts the Interaction with SKM in the terminal
        /// </summary>
        public void Start()
        {
            Console.WriteLine("""
                The Stella Knowledge Manager provides agnostic way to recalling knowledge.
                It implements Spaced-Repetition for files.
                It implements a priority property for files to learn. (You always learn the highest prority items first).
                It implements cram-mode, where you can learn files without a due date instead being only based on their given priority.

                """
            );
            Console.WriteLine("Press Ctrl+C to quit the program");
            Console.WriteLine("Its safe to quit the program at any time, saves and backups are done frequently and are located at \nappdata\\roaming\\Stella Knowledge Manager \n");

            while (true)
            {
                Console.WriteLine("Do you want to (s)how, (l)earn ,(a)dd/(e)dit items?");

                string choice = Console.ReadLine().ToLower();
                switch (choice)
                {
                    case "e":
                        Console.WriteLine("Do you want to (d)elete, (c)hange an item?");
                        string editChoice = Console.ReadLine().ToLower();

                        switch (editChoice)
                        {
                            case "d":
                                Console.WriteLine("Do you want to delete based on its id? (Y)es/(N)");
                                string deleteItemByIDResponse = Console.ReadLine().ToLower();

                                if (deleteItemByIDResponse == "y")
                                {
                                    Console.WriteLine("Do you wish to display all items so you can see what you want to delete? (Y)es/(N)o");
                                    var showAllItemsResponseD = Console.ReadLine().ToLower();

                                    if (showAllItemsResponseD == "y")
                                    {

                                    }
                                    Console.WriteLine("Which item based on its id do you want to delete?");
                                    string id = Console.ReadLine().ToLower();

                                    if (id != "")
                                    {
                                        Guid guideId = new(id);
                                        DeleteItem(guideId);
                                        Console.WriteLine($"Deleting Item with the ID of {id}");
                                    }
                                    break;
                                }
                                Console.WriteLine("Do you want to delete all items? (Y)es/(N)");
                                string deleteAllItemsResponse = Console.ReadLine().ToLower();
                                if (deleteAllItemsResponse == "y")
                                {
                                    break;
                                }
                                break;
                            case "c":
                                Console.WriteLine("Do you wish to display all items so you can see what you want to change?");
                                var showAllItemsResponseC = Console.ReadLine().ToLower();

                                if (showAllItemsResponseC == "y")
                                {

                                }
                                Console.WriteLine("Enter the id of an item you want to change");
                                var itemIdToBeChanged = Console.ReadLine();

                                Console.WriteLine("What do you wish to change?");
                                Console.WriteLine("(N)ame, (P)rority, (D)escription or (F)ile-Path");

                                var whatToChangeResponse = Console.ReadLine().ToLower();

                                switch (whatToChangeResponse)
                                {
                                    case "n":
                                        Console.WriteLine("pls type the new name");
                                        break;
                                    case "p":
                                        Console.WriteLine("pls type the new priority");
                                        break;
                                    case "d":
                                        Console.WriteLine("pls type the new descrption");
                                        break;
                                    case "f":
                                        Console.WriteLine("pls type the new file-path");
                                        string newFilePath = Console.ReadLine();
                                        break;
                                }
                                Console.WriteLine("Change Item Data");
                                break;
                        }

                        break;
                    case "s":
                        SpaceRepetitionDatabaseClient.SendFrame("""
                            {
                                "Command": "RETRIVE_ALL_ITEMS"
                            }
                            """);

                        string all_items_string = SpaceRepetitionDatabaseClient.ReceiveFrameString();
                        List<FileToLearn> all_items = JsonSerializer.Deserialize<List<FileToLearn>>(all_items_string);

                        foreach (var item in all_items)
                        {
                            Console.WriteLine(item.PrettyPrint());
                        }
                        break;
                    case "l":

                        Console.WriteLine("Do you wish to learn via Spaced Repetition? (Y)es/(N)o");
                        string SRSResponse = Console.ReadLine().ToLower();

                        if (SRSResponse == "y")
                        {
                            PriorityLearning();
                        }
                        else
                        {
                            Console.WriteLine("Do you wish to learn via Cram Mode (Ignoring Due Dates)? (Y)es/(N)o");
                            string crameModeResponse = Console.ReadLine().ToLower();

                            if (crameModeResponse == "y")
                            {
                            }
                        }
                        break;
                    case "a":
                        Console.WriteLine("Enter the name of the item:");
                        string name = Console.ReadLine().Trim('"');

                        Console.WriteLine("Enter the description of the item (What goal to you want to achieve when learning this item?):");
                        string description = Console.ReadLine();

                        Console.WriteLine("Enter the full file path:");
                        string filePath = Console.ReadLine().Trim('"');

                        while (!File.Exists(filePath)) // Update
                        {
                            Console.WriteLine("File does not exist pls choose a file that exists");
                            filePath = Console.ReadLine().Trim('"');
                        }

                        Console.WriteLine("Enter the priority (optional, press Enter to use default):");
                        string priorityInput = Console.ReadLine().Trim('"');

                        float priority = 0f; // Default priority
                        if (!string.IsNullOrEmpty(priorityInput))
                        {
                            if (!float.TryParse(priorityInput, out priority))
                            {
                                Console.WriteLine("Invalid priority value. Using default.");
                            }
                        }

                        FileToLearn newFileToLearn = new(Guid.NewGuid(), name, filePath, description, 2.5, priority);

                        AddNewFile(newFileToLearn);
                        Console.WriteLine("Item added!");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private void LogMessage(string message)
        {
            LoggerClient.SendFrame($"{{ \"Message\": \"{message}\" }}");
        }
    }
}
