using System;
using System.Collections.Generic;
using System.Linq;

namespace CLI
{
    public class Commands : List<Command>
    {
        public string Prompt { get; set; } = "";
        public bool TerminateOnUnknownCommand { get; set; } = true;

        private Scripting StartupScripts { get; }
        private Scripting RuntimeScripts { get; }

        private ExecFunc ExecDelegate { get; }

        public Commands()
        {
            ExecDelegate = new ExecFunc((name, context) =>
            {
                name = name.Split('.').Aggregate((s, e) => s + " " + e);

                Command command = Find(c => c.Name == name);
                if (command == null)
                {
                    Console.WriteLine("Command {0} not found!", name);
                    return;
                }

                command.Run(context);
            });

            StartupScripts = new Scripting(ExecDelegate, true);
            RuntimeScripts = new Scripting(ExecDelegate);

            Command.SetCommandsReference(this);
        }

        private void Write(string message = null, params object[] args)
        {
            if (message != null)
            {
                Console.WriteLine(message, args);
            }
            Console.Write(Prompt);
        }

        private Command FindCommand(List<string> args, bool strict = false)
        {
            if (args.Count == 0)
            {
                return null;
            }

            string cmd = "";
            Command command = null;

            while (args.Count > 0)
            {
                cmd += (cmd.Length > 0 ? " " : "") + args[0];
                args.RemoveAt(0);

                command = Find(c => c.Name == cmd);
                if (command != null)
                {
                    break;
                }
            }

            if (command == null)
            {
                Console.WriteLine($"Command '{cmd}' not found!");

                if (strict || TerminateOnUnknownCommand)
                {
                    Environment.Exit(-1);
                }
            }

            return command;
        }

        private void RunStartupCommand(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            List<string> argsl = args.ToList();
            Command command = FindCommand(argsl, true);

            if (command.Handler == null)
            {
                Console.WriteLine($"Command '{command.Name}' has no action!");
                Environment.Exit(-1);
                return;
            }

            Context context = new Context(argsl.ToArray());

            try
            {
                command.Handler.Invoke(context);
                Environment.Exit(0);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(-1);
            }
        }

        private void Run(string input)
        {
            string name = "";
            string rawArgs = "";

            int k = input.IndexOf(" -");
            if (k > -1)
            {
                name = input.Substring(0, k);
                rawArgs = input.Substring(k + 1);
            }
            else
            {
                name = input;
            }

            Script script = RuntimeScripts.FindScript(name);
            if(script != null)
            {
                RuntimeScripts.RunScript(script, new Context(rawArgs));
                return;
            }

            List<string> argsl = input.Split(' ').ToList();
            Command command = FindCommand(argsl);
            if (command == null)
            {
                return;
            }

            Context context = new Context(argsl.ToArray());
            command.Run(context);
        }

        private void PrintCommands()
        {
            ForEach(command =>
            {
                Console.WriteLine("\t> " + command.Name);
                if (command.Description != null)
                {
                    Console.WriteLine("\t\t" + command.Description);
                }
                if (command.Example != null)
                {
                    Console.WriteLine("\t\texample: " + command.Example);
                }
                if (command.Examples != null)
                {
                    foreach(string example in command.Examples)
                    {
                        Console.WriteLine("\t\texample: " + example);
                    }
                }
            });
            Write();
        }

        private bool RunsInternally(string input)
        {
            switch (input)
            {
                case null:
                    return true;
                case "help":
                    PrintCommands();
                    return true;
                case "clear":
                    Console.Clear();
                    Write();
                    return true;
                case "exit":
                    Environment.Exit(0);
                    return true;
                default:
                    return false;
            }
        }

        private string GetInput()
        {
            string input = Console.ReadLine();
            return input?.Trim();
        }

        private void ClearCurrentLine()
        {
            int clearWidth = Console.CursorLeft;
            string clearString = new string(' ', clearWidth);
            Console.CursorLeft = 0;
            Console.Write(Prompt + clearString);
            Console.CursorLeft = Prompt.Length;
        }

        public void Initialize(string[] args = null)
        {
            StartupScripts.RunAllScripts();

            if (args?.Length > 0)
            {
                if (args[0] == "--help")
                {
                    PrintCommands();
                    Environment.Exit(0);
                }
                RunStartupCommand(args);
            }

            Console.CancelKeyPress += (o, e) =>
            {
                Environment.Exit(0);
            };

            Write();

            string input = "";
            while ((input = GetInput()) != "exit")
            {
                if (RunsInternally(input)) continue;
                Run(input);
                Write();
            }
        }

        public void UseStartupScript(string scriptPath)
        {
            StartupScripts.AddScript(scriptPath);
        }

        public void UseScript(string scriptPath)
        {
            RuntimeScripts.AddScript(scriptPath);
        }
    }

}
