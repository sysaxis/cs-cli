using System;

using CLI;

namespace CLISample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "cli sample";

            var commands = new Commands(true)
            {
                Prompt = "$ "
            };

            new SampleCommands();

            // start command line loop
            commands.Initialize();
        }
    }
}
