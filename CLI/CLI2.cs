using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace CLI
{
    internal class History : IDisposable
    {
        private bool IsDisposing { get; set; }

        private string HPath { get; set; }

        private FileStream HStream { get; set; }
        private StreamWriter WriteStream { get; set; }

        private List<string> HBuffer = new List<string>() { };
        
        private bool CanWrite => HStream != null && HStream.CanWrite;

        private int ReadOffset { get; set; }

        public History(string name)
        {
            string workingDir = Directory.GetCurrentDirectory();

            HPath = Path.Combine(workingDir, name);
            
            try
            {
                HStream = new FileStream(HPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            catch
            {
                return;
            }

            ReadToBuffer();

            HStream.Seek(0, SeekOrigin.End);
            ReadOffset = 0;
            
            WriteStream = new StreamWriter(HStream, Encoding.UTF8, 256, true);
        }

        private void ReadToBuffer()
        {
            HStream.Seek(0, SeekOrigin.Begin);
            using (StreamReader streamReader = new StreamReader(HStream, Encoding.UTF8, false, 1024, true))
            {
                string hline;
                while((hline = streamReader.ReadLine()) != null)
                {
                    HBuffer.Add(hline);
                }
            }
            HStream.Seek(0, SeekOrigin.End);
        }

        public void Write(string input)
        {
            if (!CanWrite) return;
            WriteStream.WriteLine(input);
            WriteStream.Flush();

            HBuffer.Add(input);
            ReadOffset = 0;
        }

        public string ReadPrev()
        {
            if (HBuffer.Count == 0) return null;
            if (ReadOffset < HBuffer.Count - 1) ReadOffset++;
            return HBuffer[ReadOffset];
        }

        public string ReadNext()
        {
            if (HBuffer.Count == 0) return null;
            if (ReadOffset > 0) ReadOffset--;
            return HBuffer[ReadOffset];
        }

        public void Clear()
        {
            WriteStream.Close();
            HStream.Close();
            File.Delete(HPath);

            HStream = new FileStream(HPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            ReadOffset = 0;
            WriteStream = new StreamWriter(HStream, Encoding.UTF8, 256, true);
            HBuffer.Clear();
        }

        public void Dispose()
        {
            if (IsDisposing) return;
            HStream.Close();
            WriteStream.Dispose();
        }
    }

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
            string[] parts = input.Split(' ');

            Args args = new Args();

            int k = -1;

            Arg arg = null;
            while (++k < parts.Length)
            {
                string part = parts[k];
                if (part.StartsWith("--"))
                {
                    string[] subparts = part.Substring(1).Split('=');
                    arg = new Arg
                    {
                        Tag = subparts[0]
                    };

                    if (subparts.Length == 2)
                    {
                        arg.RawValue = subparts[1];
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
                    arg.RawValue = part;
                }
            }

            return args;
        }
    }

    public class Command
    {
        public string Namespace { get; set; }
        public Action<Args> Handler { get; set; }

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
        public string HistoryFileName { get; set; } = ".clihistory";

        private History History { get; set; }

        public Commands(bool enableHistory = false)
        {
            Command.SetCommandsReference(this);

            if (enableHistory)
            {
                History = new History(HistoryFileName);
            }
        }

        private void Write(string message = null, params object[] args)
        {
            if (message != null)
            {
                Console.WriteLine(message, args);
            }
            Console.Write(Prompt);
        }

        private void Run(string input)
        {
            if (input.Length > 0)
            {
                History?.Write(input);
            }

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
                Console.WriteLine("\t" + command.Namespace);
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
                case "hclear":
                    History.Clear();
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

        public void Initialize()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                Environment.Exit(0);
            };

            Write();
            
            ConsoleKeyInfo consoleKeyInfo;
            string hline = null;
            string currentLine = "";
            while (true)
            {
                consoleKeyInfo = Console.ReadKey();

                switch (consoleKeyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        string input = currentLine;
                        if (input == "")
                        {
                            Write(Prompt);
                            continue;
                        }

                        currentLine = "";

                        if (RunsInternally(input)) continue;
                        Write("");
                        Run(input);
                        break;
                    case ConsoleKey.UpArrow:
                        Console.CursorLeft = Prompt.Length;
                        hline = History.ReadPrev();
                        if (hline != null)
                        {
                            ClearCurrentLine();
                            currentLine = hline;
                            Console.Write(hline);
                            Console.CursorLeft = Prompt.Length + hline.Length;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        Console.CursorLeft = Prompt.Length;
                        hline = History.ReadNext();
                        if (hline != null)
                        {
                            ClearCurrentLine();
                            currentLine = hline;
                            Console.Write(hline);
                            Console.CursorLeft = Prompt.Length + hline.Length;
                        }
                        break;
                    default:
                        if (!char.IsControl(consoleKeyInfo.KeyChar))
                        {
                            currentLine += consoleKeyInfo.KeyChar;
                        }
                        break;
                }
            }
            
        }
    }

}
