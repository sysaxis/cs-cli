using System;
using System.Collections.Generic;
using System.Text;

using CLI;

namespace CLISample
{

    public class SampleCommands
    {
        public SampleCommands()
        {
            new Command
            {
                Name = "demo cli",
                Description = "pass different args to showcase arg parsing",
                Examples = new string[] {
                    "demo cli -a 12 --b=\"hello friend\"",
                    "demo cli -b hello -c friend"
                },
                Handler = args =>
                {
                    var num = args.Get<int>("a");
                    var str = args.Get("b");
                    var str2 = args.Get("c");

                    Console.WriteLine("a=" + num);
                    Console.WriteLine("b=" + str);

                    if (str2 != null)
                    {
                        Console.WriteLine("c=" + str2);
                    }
                }
            };

            new Command
            {
                Name = "sample two",
                Handler = args =>
                {
                    Console.WriteLine("i am sample two!");
                }
            };

            new Command
            {
                Name = "demo no handler",
                Description = "run this command during startup to receive exit code -1"
            };

            new Command
            {
                Name = "ask",
                Description = "ask a question",
                Handler = args =>
                {
                    string question = args.Get("q", "question");
                    if (args.HasFlag("print"))
                    {
                        Console.WriteLine("Q: " + question + "?");
                    }

                    Console.WriteLine("A: I don't know!");
                }
            };

            new Command
            {
                Name = "sample async",
                Description = "Sample command with async delegate",
                AsyncHandler = async args =>
                {
                    Console.WriteLine("getting back to you in a sec...");
                    await System.Threading.Tasks.Task.Delay(1000);
                    Console.WriteLine("done");
                }
            };
        }
    }
}
