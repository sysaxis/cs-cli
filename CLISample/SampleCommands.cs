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
                Namespace = "test cli",
                Handler = (args) =>
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
            
        }
    }
}
