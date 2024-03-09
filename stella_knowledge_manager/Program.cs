namespace stella_knowledge_manager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("EXPLAINING WHAT THE MAIN GOAL OF THE STELLA KNOWLEDGE MANAGER IS");
            
            
            
            
            
            SKM skm = new ();

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
                                        skm.RemoveItem(id);
                                        Console.WriteLine($"Deleting Item with the ID of {id}");
                                    }
                                    break;
                                }
                                Console.WriteLine("Do you want to delete all items? (Y)es/(N)");
                                string deleteAllItemsResponse = Console.ReadLine().ToLower();
                                if(deleteAllItemsResponse == "y")
                                {
                                    skm.Clear();
                                    break;
                                }
                                break;
                            case "c":
                                break;
                        }

                        break;
                    case "s":
                        skm.ShowAllItems();
                        break;
                    case "l":
                        skm.StartLearning(); // Call your existing learning logic function
                        break;
                    case "a":
                        Console.WriteLine("Enter the name of the item:");
                        string name = Console.ReadLine().Trim('"');

                        Console.WriteLine("Enter the full file path:");
                        string filePath = Console.ReadLine().Trim('"');
                        
                        while (!File.Exists(filePath)) // Update
                        {
                            Console.WriteLine("File does not exist pls choose a file that exists");
                            filePath = Console.ReadLine().Trim('"');
                        }

                        Console.WriteLine("Enter the priority (optional, press Enter to use default):");
                        string priorityInput = Console.ReadLine().Trim('"');

                        float priority = 2.5f; // Default priority
                        if (!string.IsNullOrEmpty(priorityInput))
                        {
                            if (!float.TryParse(priorityInput, out priority))
                            {
                                Console.WriteLine("Invalid priority value. Using default.");
                            }
                        }

                        skm.AddItem(name, filePath, priority);
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
    }
}
