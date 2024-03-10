using System.Diagnostics;

namespace stella_knowledge_manager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("EXPLAINING WHAT THE MAIN GOAL OF THE STELLA KNOWLEDGE MANAGER IS");
            

            SKM skm = new ();
            skm.LoadData();
            Console.WriteLine("Press Ctrl+D to quit the program");

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
                                
                                if(deleteItemByIDResponse == "y")
                                {
                                    Console.WriteLine("Which item based on its id do you want to delete?");
                                    string id = Console.ReadLine().ToLower();
                                    
                                    if (id != "")
                                    {
                                        Guid guideId = new (id);
                                        skm.RemoveItem(guideId);
                                        Console.WriteLine($"Deleting Item with the ID of {id}");
                                        skm.SaveData();
                                    }
                                    break;
                                }
                                Console.WriteLine("Do you want to delete all items? (Y)es/(N)");
                                string deleteAllItemsResponse = Console.ReadLine().ToLower();
                                if(deleteAllItemsResponse == "y")
                                {
                                    skm.ClearPriorityList();
                                    break;
                                }
                                break;
                            case "c":
                                break;
                        }

                        break;
                    case "s":
                        Console.WriteLine("Show all items (Y)es or (No) and search for specifc name?");
                        string showItemsResponse = Console.ReadLine().ToLower();
                        if(showItemsResponse == "y")
                        {
                            SKMUtil.ShowAllItems(skm);
                        }
                        if(showItemsResponse == "n")
                        {
                            Console.WriteLine("Please specify a name to search by");
                            string searchString = Console.ReadLine().ToLower();

                            foreach (var item in SKMUtil.SearchAndSort(skm, searchString))
                            {
                                item.PrettyPrint();
                            }
                        }
                        break;
                    case "l":
                        StartLearning(skm); // Call your existing learning logic function
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

                        float priority = 100f; // Default priority
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
            /*
            Console.WriteLine(item.LastReviewDate);

            // The user should be asked to increase, decrease the prority value
            // or do not change anything
            Console.WriteLine("Do you want to change the prority of the item?");
            Console.WriteLine("(Y)es | (N)o");

            if (Console.ReadLine() == "N")
            {
                Console.WriteLine("You selected to NOT change the prority value of FILENAME");
            };
            */
        }

        public static void StartLearning(SKM skm)
        {
            foreach (var item in skm.PriorityList)
            {
                bool itemIsDue = true;

                if (item.NextReviewDate >= DateTime.Now)
                {
                    itemIsDue = false;
                }

                if (!File.Exists(item.PathToFile))
                {
                    Console.WriteLine("File not found. Please try again.");
                    return;
                }
                if (itemIsDue)
                {
                    try
                    {
                        // Start the process
                        //var process = new Process();
                        //process.StartInfo = new ProcessStartInfo(item.PathToFile) { UseShellExecute = true };
                        //process.Start();
                        //Process.Start("explorer.exe", item.PathToFile);
                        // Wait for the process to exit

                        Console.WriteLine($"Start Learning the item {item.Name}");

                        Console.WriteLine("DISPLAY DESCRIPTION (WHAT DO YOU WANT TO ACHIEVE WITH LEARNING THIS ITEM?)");
                        Console.WriteLine($"Description: '{item.Description}'");
                        Console.WriteLine("Press Enter To Open The File to Continue...");
                        Console.ReadLine().ToLower();

                        Process.Start("explorer.exe", item.PathToFile);

                        Console.WriteLine("Now Give Yourself feedback how well did you achieve your goal?");
                        Console.WriteLine("Enter 'g' for good, 'b' for bad, or 'r' to do it again:");

                        string input = Console.ReadLine().ToLower(); // Read input and convert to lowercase

                        if (input == "g")
                        {
                            Console.WriteLine("Great! Let's move on.");
                            item.NextReviewDate = SpacedRepetitionScheduler.CalculateNextReviewDate(item, RecallEvaluation.GOOD);
                        }
                        else if (input == "b")
                        {
                            Console.WriteLine("Don't worry, we can try again later.");
                            item.NextReviewDate = SpacedRepetitionScheduler.CalculateNextReviewDate(item, RecallEvaluation.BAD);

                        }
                        else if (input == "r")
                        {
                            Console.WriteLine("Let's try that again.");
                            item.NextReviewDate = SpacedRepetitionScheduler.CalculateNextReviewDate(item, RecallEvaluation.AGAIN);
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Please enter 'g', 'b', or 'r'.");
                        }

                        Console.WriteLine($"New Due Date: {item.NextReviewDate}");
                        item.NumberOfTimeSeen += 1;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Item: '{item.Name}' next review day is {item.NextReviewDate}");
                }
            }
            skm.SaveData();
        }
    }
}
