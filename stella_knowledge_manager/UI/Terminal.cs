using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stella_knowledge_manager.UI
{
    /// <summary>
    /// Represents the Terminal UI for interacting with SKM
    /// </summary>
    public class Terminal
    {
        /// <summary>
        /// This Learning session ignores Due Dates
        /// </summary>
        /// <param name="skm"></param>
        public static void CramLearning(SKM skm)
        {
            Stack<ISRSItem> itemStack = new();
            // We reverse the list before we push the element onto the stack the reason for this is because
            // otherwise the priority order is reveresed we would see the lowest priority items first (Not what we want)
            skm.PriorityList.Reverse();

            foreach (var item in skm.PriorityList)
            {
                itemStack.Push(item);
            }

            while (itemStack.Count > 0)
            {
                var item = itemStack.Pop();

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
                        }
                        else if (input == "b")
                        {
                            Console.WriteLine("Don't worry, we can try again later.");
                            itemStack.Push(item);
                            validInput = true;
                        }
                        else if (input == "r")
                        {
                            Console.WriteLine("Let's try that again.");
                            itemStack.Push(item);
                            validInput = true;
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Please enter (g)ood , (b)ad , or (r)etry.");
                            validInput = false;
                        }
                        item.NumberOfTimeSeen += 1;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
                skm.SaveData();
                Console.WriteLine($"Current Priority : {item.Priority}");
                Console.WriteLine("Do you wish to change the priority of the current item? (Y)es/(N)o (Default NO)");
                var changePriorityResponse = Console.ReadLine().ToLower();

                if (changePriorityResponse == "y")
                {
                    ChangeCurrentPriority(item);
                }
                skm.SaveData();
            }
            // At the beginning we reversed the list so its correctly pushed onto the stack,
            // We should reverse this again so you priority list is in the right order
            skm.PriorityList.Reverse();
            skm.SaveData();
        }

        public static void PriorityLearning(SKM skm)
        {
            foreach (var item in skm.PriorityList)
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
                                item.NextReviewDate = AT.SRS.SpacedRepetitionScheduler.CalculateNextReviewDate(item, AT.SRS.RecallEvaluation.GOOD);
                                validInput = true;
                            }
                            else if (input == "b")
                            {
                                Console.WriteLine("Don't worry, we can try again later.");
                                item.NextReviewDate = AT.SRS.SpacedRepetitionScheduler.CalculateNextReviewDate(item, AT.SRS.RecallEvaluation.BAD);
                                validInput = true;


                            }
                            else if (input == "r")
                            {
                                Console.WriteLine("Let's try that again.");
                                item.NextReviewDate = AT.SRS.SpacedRepetitionScheduler.CalculateNextReviewDate(item, AT.SRS.RecallEvaluation.AGAIN);
                                validInput = true;
                            }
                            else
                            {
                                Console.WriteLine("Invalid input. Please enter 'g', 'b', or 'r'.");
                                validInput = false;
                            }
                        }
                        Console.WriteLine($"New Due Date: {item.NextReviewDate}");
                        item.NumberOfTimeSeen += 1;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                    }

                    skm.SaveData();
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
                }
                skm.SaveData();
                Console.WriteLine($"Item: '{item.Name}' next review day is {item.NextReviewDate}");
            }
            skm.SaveData();
        }

        // You should be able to give an exercise and let the user give an answer and the program should calculate if the 
        // Answer was good. For example, write a abstract base class in C++, you then get an answer on how good you did.
        // This could be a good place for a LLM 

        /// <summary>
        /// Prints information about the item to learn and starts the process
        /// </summary>
        /// <param name="item"></param>
        public static void LearnItem(ISRSItem item)
        {
            if (!File.Exists(item.PathToFile))
            {
                Console.WriteLine($"No File with the file-path {item.PathToFile} found");
                return;
            }

            Console.WriteLine($"Start Learning the item {item.Name}");
            Console.WriteLine($"Description: '{item.Description}'");
            Console.WriteLine($"Press Enter To Open The File {item.PathToFile} to Continue...");
            Console.ReadLine().ToLower();

            Process.Start("explorer.exe", item.PathToFile);

            Console.WriteLine("Now Give Yourself feedback how well did you achieve your goal?");
            Console.WriteLine("Enter 'g' for good, 'b' for bad, or 'r' to do it again:");
        }

        public static void ChangeCurrentPriority(SKM skm)
        {
            double newPriority = 0;

            Console.WriteLine("Type the id of the item where you want to change the priority");
            string itemId = Console.ReadLine();

            Console.WriteLine("Type the new priority you want to assign");
            bool parsingPrioritySuccess = double.TryParse(Console.ReadLine().ToLower(), out newPriority);


            if (parsingPrioritySuccess)
            {
                SKMUtil.GetItemById(skm, new Guid(itemId)).Priority = newPriority;
            }
        }

        public static void ChangeCurrentPriority(ISRSItem item)
        {
            double newPriority = 0;

            Console.WriteLine("Type the new priority you want to assign");
            bool parsingPrioritySuccess = double.TryParse(Console.ReadLine().ToLower(), out newPriority);

            if (parsingPrioritySuccess)
            {
                item.Priority = newPriority;
            }
        }

        /// <summary>
        /// Here we want to implement the ability to take a quiz
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void TakeQuiz()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Here we want to implement the ability to take a quiz
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void TakeFlashCards()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Here we want to implement the ability to take a quiz
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public static void TakeCloze()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts the Interaction with SKM in the terminal
        /// </summary>
        public static void Start()
        {
            Console.WriteLine("""
                The Stella Knowledge Manager provides agnostic way to recalling knowledge.
                It implements Spaced-Repetition for files.
                It implements a priority property for files to learn. (You always learn the highest prority items first).
                It implements cram-mode, where you can learn files without a due date instead being only based on their given priority.

                """);


            SKM skm = new();
            skm.LoadData();
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
                                        SKMTerminalUtil.ShowAllItems(skm);

                                    }
                                    Console.WriteLine("Which item based on its id do you want to delete?");
                                    string id = Console.ReadLine().ToLower();

                                    if (id != "")
                                    {
                                        Guid guideId = new(id);
                                        skm.RemoveItem(guideId);
                                        Console.WriteLine($"Deleting Item with the ID of {id}");
                                        skm.SaveData();
                                    }
                                    break;
                                }
                                Console.WriteLine("Do you want to delete all items? (Y)es/(N)");
                                string deleteAllItemsResponse = Console.ReadLine().ToLower();
                                if (deleteAllItemsResponse == "y")
                                {
                                    skm.ClearPriorityList();
                                    break;
                                }
                                break;
                            case "c":
                                Console.WriteLine("Do you wish to display all items so you can see what you want to change?");
                                var showAllItemsResponseC = Console.ReadLine().ToLower();

                                if (showAllItemsResponseC == "y")
                                {
                                    SKMTerminalUtil.ShowAllItems(skm);

                                }
                                Console.WriteLine("Enter the id of an item you want to change");
                                var itemIdToBeChanged = Console.ReadLine();

                                var itemToChange = SKMUtil.GetItemById(skm, new Guid(itemIdToBeChanged));
                                itemToChange.PrettyPrint();

                                Console.WriteLine("What do you wish to change?");
                                Console.WriteLine("(N)ame, (P)rority, (D)escription or (F)ile-Path");

                                var whatToChangeResponse = Console.ReadLine().ToLower();

                                switch (whatToChangeResponse)
                                {
                                    case "n":
                                        Console.WriteLine("pls type the new name");
                                        itemToChange.Name = Console.ReadLine();
                                        break;
                                    case "p":
                                        Console.WriteLine("pls type the new priority");
                                        itemToChange.Priority = double.Parse(Console.ReadLine());
                                        break;
                                    case "d":
                                        Console.WriteLine("pls type the new descrption");
                                        itemToChange.Description = Console.ReadLine();
                                        break;
                                    case "f":
                                        Console.WriteLine("pls type the new file-path");
                                        string newFilePath = Console.ReadLine();
                                        if (Path.Exists(newFilePath))
                                        {
                                            itemToChange.PathToFile = newFilePath;
                                        }
                                        break;
                                }
                                Console.WriteLine("Change Item Data");
                                itemToChange.PrettyPrint();
                                skm.SaveData();
                                break;
                        }

                        break;
                    case "s":
                        SKMTerminalUtil.ShowAllItems(skm);
                        break;
                    case "l":

                        Console.WriteLine("Do you wish to learn via Spaced Repetition? (Y)es/(N)o");
                        string SRSResponse = Console.ReadLine().ToLower();

                        if (SRSResponse == "y")
                        {
                            PriorityLearning(skm);
                        }
                        else
                        {
                            Console.WriteLine("Do you wish to learn via Cram Mode (Ignoring Due Dates)? (Y)es/(N)o");
                            string crameModeResponse = Console.ReadLine().ToLower();

                            if (crameModeResponse == "y")
                            {
                                CramLearning(skm);
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

                        skm.AddItem(name, description, filePath, priority);
                        skm.SaveData();
                        Console.WriteLine("Item added!");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }
    }
}
