using System;
using System.IO;

using CLI;

namespace CLISample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "cli sample";

            var commands = new Commands()
            {
                Prompt = "$ "
            };

            commands.UseStartupScript("./Scripts/startup1.txt");
            commands.UseScript("./Scripts/test1.txt");

            new SampleCommands();
            new CalculatorCommands();
            new FsCommands();

            new Command
            {
                Name = "echo",
                Handler = a =>
                {
                    Console.WriteLine(a.Get("message"));
                }
            };

            // start command line loop
            commands.Initialize(args);
        }
    }
}
