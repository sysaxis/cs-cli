using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace CLI
{
    public class Arg
    {
        public string Tag { get; set; }
        public object RawValue { get; set; }
    }

    public class Args : List<Arg>
    {
        public T? Get<T>(string tag) where T : struct
        {
            var arg = Find(a => a.Tag == tag);
            if (arg?.RawValue == null) return null;
            return (T)Convert.ChangeType(arg.RawValue, typeof(T));
        }

        public T? Get<T>(params string[] tags) where T : struct
        {
            T? result = null;
            foreach (var tag in tags)
            {
                result = (T?)Get<T>(tag);
                if (result != null) break;
            }
            return result;
        }

        public T Get<T>(T defaultValue, params string[] tags) where T : struct
        {
            T? result = null;
            foreach (var tag in tags)
            {
                result = (T?)Get<T>(tag);
                if (result != null) break;
            }
            return (result != null && result.HasValue) ? result.Value : defaultValue;
        }

        public string Get(string tag)
        {
            var arg = Find(a => a.Tag == tag);
            return (string)arg?.RawValue;
        }

        public string Get(params string[] tags)
        {
            string result = null;
            foreach (var tag in tags)
            {
                result = Get(tag);
                if (result != null) break;
            }
            return result;
        }

        public bool HasParam(string tag)
        {
            var arg = Find(a => a.Tag == tag);
            return (arg != null && arg.RawValue != null);
        }

        public bool HasAnyParam(params string[] tags)
        {
            return tags.Any(HasParam);
        }

        public static Args Parse(string input)
        {
            string[] parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return Parse(parts);
        }

        public static Args Parse(string[] parts)
        {
            Args args = new Args();

            int k = -1;

            Arg arg = null;
            while (++k < parts.Length)
            {
                string part = parts[k];
                if (part.StartsWith("--"))
                {
                    string[] subparts = part.Substring(2).Split('=');
                    arg = new Arg
                    {
                        Tag = subparts[0]
                    };

                    if (subparts.Length == 2)
                    {
                        if (subparts[1].StartsWith("\""))
                        {
                            arg.RawValue = subparts[1].Substring(1);
                            while (++k < parts.Length)
                            {
                                if (parts[k].EndsWith("\""))
                                {
                                    arg.RawValue += " " + parts[k].Substring(0, parts[k].Length - 1);
                                    break;
                                }
                                else
                                {
                                    arg.RawValue += " " + parts[k];
                                }
                            }
                        }
                        else
                        {
                            arg.RawValue = subparts[1];
                        }
                    }
                    args.Add(arg);
                    arg = null;
                }
                else if (part.StartsWith("-"))
                {
                    arg = new Arg
                    {
                        Tag = part.Substring(1)
                    };
                    args.Add(arg);
                }
                else if (arg != null)
                {
                    if (part.StartsWith("\""))
                    {
                        arg.RawValue = part.Substring(1);
                        while(++k < parts.Length)
                        {
                            if (parts[k].EndsWith("\""))
                            {
                                arg.RawValue += " " + parts[k].Substring(0, parts[k].Length - 1);
                                break;
                            }
                            else
                            {
                                arg.RawValue += " " + parts[k];
                            }
                        }
                    }
                    else
                    {
                        arg.RawValue = part;
                    }
                }
            }

            return args;
        }
    }

    public class Command
    {
        public string Namespace { get; set; }
        public Action<Args> Handler { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
        public string[] Examples { get; set; }

        private static Commands CommandsRef;

        public Command()
        {
            CommandsRef?.Add(this);
        }

        public static void SetCommandsReference(Commands commands)
        {
            CommandsRef = commands;
        }

        public void Run(Args args)
        {
            try
            {
                if (Handler == null)
                {
                    Console.WriteLine("Command \"{0}\" has no action!", Namespace);
                    return;
                }
                Handler.Invoke(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Command \"{0}\" returned with an exception: {1}", Namespace, ex.Message);
                Console.WriteLine("Stacktrace {0}", ex.StackTrace);
            }
        }

    }

    public class Commands : List<Command>
    {
        public string Prompt { get; set; } = "";
        
        public Commands(bool enableHistory = false)
        {
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
            string @namespace = "";

            int k = input.IndexOf(" -");
            if (k > -1)
            {
                @namespace = input.Substring(0, k);
            }
            else
            {
                @namespace = input;
            }

            Command command = Find(c => c.Namespace == @namespace);
            if (command == null)
            {
                return;
            }

            if (command.Handler == null)
            {
                Console.WriteLine($"Command '{@namespace}' has no action!");
                Environment.Exit(-1);
                return;
            }

            Args arguments = Args.Parse(args);
            try
            {
                command.Handler.Invoke(arguments);
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
            string @namespace = "";
            string rawArgs = "";

            int k = input.IndexOf(" -");
            if (k > -1)
            {
                @namespace = input.Substring(0, k);
                rawArgs = input.Substring(k + 1);
            }
            else
            {
                @namespace = input;
            }

            Command command = Find(c => c.Namespace == @namespace);
            if (command == null)
            {
                Console.WriteLine("Command {0} not found!", @namespace);
                return;
            }

            Args args = Args.Parse(rawArgs);
            command.Run(args);
        }

        private void PrintCommands()
        {
            ForEach(command =>
            {
                Console.WriteLine("\t> " + command.Namespace);
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
            return input;
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
    }

}
