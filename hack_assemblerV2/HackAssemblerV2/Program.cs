namespace HackAssemblerV2
{
    internal class Program
    {
        /// <summary>
        /// Args[0] = INPUT_PATH    : string
        /// Args[1] = OUTPUT_PATH   : string
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            StreamReader streamReader = new (args[0]);

            string input = streamReader.ReadToEnd();

            Assembler assembler = new(input);

            // Starting time to know how long a binary took to be generated
            // (Excludes the time it took the time to write the binary to disk)
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();
            
            Console.WriteLine("Starting Generation of Binary for the Hack-16 Bit CPU (Nand2Tetris)");
            {
                List<string> output = assembler.Assemble();

                watch.Stop();

                StreamWriter streamWriter = new(args[1]);
                foreach (var line in output)
                {
                    streamWriter.WriteLine(line);
                }

                streamWriter.Close();
            }

            Console.WriteLine($"Done in: {watch.ElapsedMilliseconds} ms");
        }
    }
}
