﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CLI
{
    public class Commands : List<Command>
    {
        public string Prompt { get; set; } = "";

        private Scripting StartupScripts { get; }
        private Scripting RuntimeScripts { get; }

        private ArgsParser DefaultArgsParser => new ArgsParser(new ArgsParserOptions
        {
            ArgPrefixes = new string[] { "--", "-" },
            KeyValOps = new string[] { "=" }
        });

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

        private void RunStartupCommand(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            string input = args.Aggregate((s, e) => s + " " + e);
            string name = "";

            int k = input.IndexOf(" -");
            if (k > -1)
            {
                name = input.Substring(0, k);
            }
            else
            {
                name = input;
            }

            Command command = Find(c => c.Name == name);
            if (command == null)
            {
                Console.WriteLine($"Command '{name}' not found!");
                Environment.Exit(-1);
                return;
            }

            if (command.Handler == null)
            {
                Console.WriteLine($"Command '{name}' has no action!");
                Environment.Exit(-1);
                return;
            }

            Context context = new Context(args);
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

            Command command = Find(c => c.Name == name);
            if (command == null)
            {
                Console.WriteLine("Command {0} not found!", name);
                return;
            }

            Context context = new Context(rawArgs);
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
