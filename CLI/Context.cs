using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace CLI
{

    public class Context
    {
        private static ArgsParser DefaultArgsParser => new ArgsParser(new ArgsParserOptions
        {
            ArgPrefixes = new string[] { "--", "-" },
            KeyValOps = new string[] { "=" }
        });

        private ArgsParser ArgsParser { get; }

        private List<Param> Params { get; }
        private List<Param> Results { get; }

        public Context()
        {
            Params = new List<Param>();
            Results = new List<Param>();
        }

        public Context(Param[] @params)
        {
            Params = @params.ToList();
            Results = new List<Param>();
        }

        public Context(string input, ArgsParser argsParser = null) : this()
        {
            if (argsParser == null)
            {
                argsParser = DefaultArgsParser;
            }

            ArgsParser = argsParser;

            if (input != null)
            {
                AddParams(input);
            }
        }

        public Context(string[] args, ArgsParser argsParser = null) : this()
        {
            if (argsParser == null)
            {
                argsParser = DefaultArgsParser;
            }

            ArgsParser = argsParser;

            var parsedArgs = argsParser.Parse(args);

            foreach (var parsedArg in parsedArgs)
            {
                Params.Add(new Param
                {
                    Name = parsedArg.Key,
                    Value = parsedArg.Value
                });
            }
        }

        public Param[] GetParams()
        {
            return Params.ToArray();
        }

        public void AddParams(string input)
        {
            var parsedArgs = ArgsParser.Parse(input);

            foreach (var parsedArg in parsedArgs)
            {
                Params.Add(new Param
                {
                    Name = parsedArg.Key,
                    Value = parsedArg.Value
                });
            }
        }

        public void AddParams(Param[] @params)
        {
            foreach (var param in @params)
            {
                var existingParam = Params.Find(p => p.Name == param.Name);
                if (existingParam != null)
                {
                    existingParam.Value = param.Value;
                }
                else
                {
                    Params.Add(param);
                }
            }
        }

        public void RemoveParam(string tag)
        {
            int k = Params.FindIndex(p => p.Name == tag);
            Params.RemoveAt(k);
        }

        public T? Get<T>(string tag) where T : struct
        {
            var arg = Params.Find(a => a.Name == tag);
            if (arg?.Value == null) return null;
            return (T)Convert.ChangeType(arg.Value, typeof(T));
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
            var arg = Params.Find(a => a.Name == tag);
            if (arg?.Value == null)
            {
                return null;
            }
            if (arg.Value.GetType() == typeof(string))
            {
                return (string)arg.Value;
            }

            return arg.Value.ToString();
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

        public object GetRaw(string tag)
        {
            var arg = Params.Find(a => a.Name == tag);
            return arg?.Value;
        }

        public bool HasParam(string tag)
        {
            var arg = Params.Find(a => a.Name == tag);
            return (arg != null && arg.Value != null);
        }

        public bool HasAnyParam(params string[] tags)
        {
            return tags.Any(HasParam);
        }

        public bool HasFlag(string tag)
        {
            var arg = Params.Find(a => a.Name == tag);
            return (arg != null && arg.Value == null);
        }

        public bool HasAnyFlag(params string[] tags)
        {
            return tags.Any(HasFlag);
        }

        public void SetResult(string key, object value)
        {
            var existingResult = Results.Find(r => r.Name == key);
            if (existingResult != null)
            {
                existingResult.Value = value;
            }
            else
            {
                Results.Add(new Param
                {
                    Name = key,
                    Value = value
                });
            }
        }

        public Param[] GetResults(string resultKey = null)
        {
            if (resultKey != null)
            {
                return Results.ConvertAll(r => new Param
                {
                    Name = resultKey + r.Name,
                    Value = r.Value
                }).ToArray();
            }
            return Results.ToArray();
        }
    }
}
