﻿using System;
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
                Name = "test cli",
                Description = "pass different args to showcase arg parsing",
                Examples = new string[] {
                    "test cli -a 12 --b=\"hello friend\"",
                    "test cli -b hello -c friend"
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
                Name = "test no handler",
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
        }
    }
}
