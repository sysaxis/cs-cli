# CLI

Command line interface builder.

## Usage
Program.cs
```cs
using CLI;

class Program
{
    static void Main(string[] args)
    {
        Commands commands = new Commands()
        {
            Prompt = "$ "
        };

        // add your commands here
        new Command
        {
            Name = "echo",
            Handler = args =>
            {
                var num = args.Get<int>("a");
                var str = args.Get("b");

                Console.WriteLine("a=" + num);
                Console.WriteLine("b=" + str);
            }
        };

        // start command line loop
        commands.Initialize(args);
    }
}
```

Command line
```
$ echo -a 10 --b="hello friend"
$ a=10
$ b=hello friend
$
```

## Async Commands

If you want async command to be awaited by the CLI then use `AsyncHandler`:
```cs
new Command
{
    Name = "example task2",
    Description = "This command will be awaited for",
    AsyncHandler = async args => {
        Console.WriteLine("getting back to you in a sec...");
        await System.Threading.Tasks.Task.Delay(1000);
        Console.WriteLine("done");
    }
};

new Command
{
    Name = "example task1",
    Description = "This command will not be awaited for",
    Handler = async args => {
        Console.WriteLine("getting back to you in a sec...");
        await System.Threading.Tasks.Task.Delay(1000);
        Console.WriteLine("done");
    }
};
```

## Scripting

Create startup scripts and runtime scripts. Runtime scripts act as CLI commands.
Scripts can be placed in the working directory and refereced with `./myscript`.

Do not rely too much on the scripting syntax. It is subject to change in later development process.

### Comprehensive Example

Program.cs
```cs
using CLI;

class Program
{
    static void Main(string[] args)
    {
        Commands commands = new Commands(true)
        {
            Prompt = "$ "
        };

        new ServerCommands();
        new FsCommands();

        new Command
        {
            Name = "echo",
            Handler = a =>
            {
                Console.WriteLine(a.Get("message", "m"));
            }
        };

        // startup scripts are executed on startup and are not available as commands on runtime.
        commands.UseStartupScript("./Scripts/startup1.txt");

        // runtime scripts are available just like predefined commands
        commands.UseScript("./Scripts/server.dump.txt");

        commands.Initialize(args);
    }
}
```

ServerCommands.cs
```cs
using CLI;

class ServerCommands
{
    Server server = null;

    public ServerCommands() {
        new Command
        {
            Name = "server",
            Handler => context => {
                if (server == null) {
                    return;
                }

                context.SetResult("status", string.Format("running on port {0}, health: {1}", server.Port, server.Health));
                context.SetResult("logStream", server.logStream);
                context.SetResult("eventStream", server.eventStream);
            }
        };

        new Command
        {
            Name = "server run",
            Handler = context => {
                var port = context.Get<int>("port", "p");
                var host = context.Get("host", "h");

                // run server somehow
                server = new Server(port, host);
                server.Start();

                context.SetResult("logStream", server.logStream);
                context.SetResult("eventStream", server.eventStream);
            }
        };
    }
}
```

FsCommands.cs
```cs
using System.IO;
using CLI;

class FsCommands
{
    public FsCommands()
    {
        new Command
        {
            Name = "cp",
            Handler = context => {
                var src = context.Get("src", "s");
                var dst = context.Get("dst", "dest", "d");
                // recursive copy entire directories or single file
            }
        };

        new Command
        {
            Name = "savestream",
            Handler = context => {
                var src = context.Get<Stream>("src", "s");
                var dst = context.Get("dst", "dest", "d");
                // save the stream to destination file
            }
        };
    }
}
```

startup1.txt
```
# This is a comment
# Assigning an output (illustrative) and using it
server.run(port: 5000, host: "example.com") -> server1
# Start a stream piping task
pipestream(stream: $server1.logStream, file: "./server.logs")

echo(m: "Server started on port 5000. Logs are available in ./server.logs")
```

server.dump.txt
```
# $destpath is script input parameter
server() -> server

savestream(src: $server.logStream, dst: $destpath)
savestream(src: $server.eventStream, dst: $destpath)

echo(m: "server info saved")
```

Command line
```
$ server dump --destpath=./dump1
```
