# CLI

Simple command line interface builder

## Usage
Program.cs
```cs
using CLI;

class Program
{
    Commands commands = new Commands(true)
    {
        Prompt = "$ "
    };

    // add your commands here
    new Command
    {
        Namespace = "echo",
        Handler = args =>
        {
            var num = args.Get<int>("a");
            var str = args.Get("b");

            Console.WriteLine("a=" + num);
            Console.WriteLine("b=" + str);
        }
    };

    // start command line loop
    commands.Initialize();
}
```
