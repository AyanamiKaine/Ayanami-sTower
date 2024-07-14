using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CProjectMakerLogger
{
    public class Logic
    {
        public static void LogToFile(DateTime time, string filePath, string message = "")
        {
            if(filePath == null)
            {
                filePath = "./logs/log.txt";
            }

            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (StreamWriter writer = File.AppendText(filePath))
                {
                    string logLine = $"{time:yyyy-MM-dd HH:mm:ss} - {message}";
                    writer.WriteLine(logLine);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception, perhaps by logging it to the console or a different file
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public static void LogToConsole(DateTime time, string message = "")
        {
            string logLine = $"{time:yyyy-MM-dd HH:mm:ss} - {message}";
            Console.WriteLine(logLine);
        }

    }
}
