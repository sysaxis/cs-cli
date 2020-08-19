using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Linq;

[assembly: InternalsVisibleTo("CLI.Tests")]
namespace CLI
{
    public class ArgKvp
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class ArgsParserOptions
    {
        public char ArgSeparator { get; set; } = ' ';
        public string[] ArgPrefixes { get; set; }
        public string[] KeyValOps { get; set; }
    }


    /// <summary>
    /// Enables parsing args array or string to it's equivalent key-value pairs
    /// based on given arg prefixes and key-value operators.
    /// </summary>
    public class ArgsParser
    {
        char[] ArgSeparators { get; }

        List<string> ArgPrefixes { get; }
        List<string> KeyValOps { get; }

        bool HasPrefixes { get; }
        bool HasKeyValOps { get; }

        public ArgsParser(ArgsParserOptions options)
        {
            ArgSeparators = new char[] { options.ArgSeparator };

            HasPrefixes = options.ArgPrefixes?.Length > 0;
            HasKeyValOps = options.KeyValOps?.Length > 0;

            if (HasPrefixes)
            {
                ArgPrefixes = options.ArgPrefixes.ToList();
            }

            if (HasKeyValOps)
            {
                KeyValOps = options.KeyValOps.ToList();
            }
        }

        public ArgKvp[] Parse(string args)
        {
            return Parse(args.Split(ArgSeparators, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Parses the args to key-value collection.
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        public ArgKvp[] Parse(string[] parts)
        {
            List<string> partsList = parts.ToList();
            List<ArgKvp> parsedArgs = new List<ArgKvp>();

            int k = -1;
            ArgKvp arg = null;

            while (++k < partsList.Count)
            {
                string part = partsList[k];
                string key = part;

                if (HasPrefixes)
                {
                    string matchingPrefix = ArgPrefixes.Find(p => part.StartsWith(p));
                    if (matchingPrefix != null)
                    {
                        key = part.Substring(matchingPrefix.Length);
                    }
                    else
                    {
                        key = null;
                    }
                }

                if (HasKeyValOps && key != null)
                {
                    string keyValOp = KeyValOps.Find(op => key.Contains(op));
                    if (keyValOp != null)
                    {
                        string postKeyPart = key.Substring(key.IndexOf(keyValOp) + 1);
                        partsList.RemoveAt(k);
                        partsList.Insert(k, postKeyPart);
                        k--;

                        key = key.Substring(0, key.IndexOf(keyValOp));
                    }
                    else if (HasPrefixes == false)
                    {
                        key = null;
                    }
                }

                if (key != null)
                {
                    arg = new ArgKvp
                    {
                        Key = key.Trim()
                    };

                    parsedArgs.Add(arg);
                    continue;
                }

                if (arg != null)
                {
                    k += UnquoteArgVal(partsList.ToArray(), out string argval, k);
                    if (argval != null)
                    {
                        arg.Value = argval.Trim();
                        k--;
                        continue;
                    }
                    else
                    {
                        arg.Value = part.Trim();
                    }
                }
            }

            return parsedArgs.ToArray();
        }

        /// <summary>
        /// Find the value of quoted text inside given args array.
        /// Supports both single and double quotes.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        /// <param name="offset"></param>
        /// <returns>The number of args that contained the quoted text</returns>
        public static int UnquoteArgVal(string[] args, out string text, int startIndex = 0, int offset = 0)
        {
            if (args.Length < startIndex + 1)
            {
                text = null;
                return 0;
            }

            string quoteChar;
            string arg = args[startIndex].Substring(offset);
            char firstChar = arg.FirstOrDefault();

            if (firstChar == '\'' || firstChar == '"')
            {
                quoteChar = firstChar.ToString();
            }
            else
            {
                text = null;
                return 0;
            }

            string textValue = arg.Substring(1);

            if (textValue.EndsWith(quoteChar))
            {
                text = arg.Substring(1, textValue.Length - 1);
                return 1;
            }

            int k = startIndex;
            int takeCount = 1;

            while (++k < args.Length)
            {
                arg = args[k];

                if (arg.EndsWith(quoteChar))
                {
                    textValue += " " + arg.Substring(0, arg.Length - 1);
                    takeCount++;
                    break;
                }
                else
                {
                    textValue += " " + arg;
                    takeCount++;
                }
            }

            text = textValue;

            return takeCount++;
        }
    }
}
