using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("CLI.Tests")]
namespace CLI
{
    class Script
    {
        public string Name { get; set; }
        public string Filepath { get; set; }
        public string[] Lines { get; private set; }

        private void SetLines(string content)
        {
            Lines = content.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList().ConvertAll(line => line.Trim()).ToArray();
        }

        public void Reload()
        {
            string content;

            using (FileStream file = new FileStream(Filepath, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    content = reader.ReadToEnd();
                }
            }

            SetLines(content);
        }
    }

    class ParamRef
    {
        public string Target { get; set; }
        public string Key { get; set; }
    }

    class ScriptFunction
    {
        public static readonly ArgsParser ArgsParser = new ArgsParser(new ArgsParserOptions
        {
            ArgSeparator = ',',
            KeyValOps = new string[] { ":" }
        });

        public ScriptFunction(string name, string args, string outputKey = null)
        {
            Name = name;
            OutputKey = outputKey;

            Context = new Context(args, ArgsParser);

            List<ParamRef> paramRefs = new List<ParamRef>();
            foreach (var param in Context.GetParams())
            {
                string stringValue = (string)param.Value;
                if (stringValue.StartsWith("$"))
                {
                    paramRefs.Add(new ParamRef
                    {
                        Target = param.Name,
                        Key = stringValue.Substring(1)
                    });

                    Context.RemoveParam(param.Name);
                };
            }

            ParamRefs = paramRefs.ToArray();
        }

        public string Name { get; }
        public Context Context { get; }
        public string OutputKey { get; }

        public ParamRef[] ParamRefs { get;}

    }

    public delegate void ExecFunc(string name, Context context);

    class ScriptExecutor
    {
        private Context ScriptContext { get; }
        public Context OutputContext { get; }
        private List<ScriptFunction> Functions { get; }

        public ScriptExecutor(string[] lines, Context context = null)
        {
            if (context == null)
            {
                context = new Context();
            }

            ScriptContext = context;
            OutputContext = new Context();
            Functions = new List<ScriptFunction>();

            foreach (string line in lines)
            {
                string ln = line.Trim();
                if (ln.StartsWith("#")) continue;
                if (ln == "") continue;

                Regex functionRegex = new Regex("([A-Za-z0-9\\.]+)\\(([\\D\\d]*?)\\)( *(->){1} *([A-Za-z0-9\\.]+))?", RegexOptions.ECMAScript);

                Match regexMatch = functionRegex.Match(line);
                if (regexMatch.Success)
                {
                    string fnName = regexMatch.Groups[1].Captures[0].Value;
                    string fnArgs = regexMatch.Groups[2].Captures[0].Value;
                    bool hasOutputAssignment = regexMatch.Groups[4].Captures.Count > 0 && regexMatch.Groups[4].Captures[0].Value == "->";
                    string assignmentOutput = hasOutputAssignment ? regexMatch.Groups[5].Captures[0].Value : null;

                    var function = new ScriptFunction(fnName, fnArgs, assignmentOutput);

                    Functions.Add(function);
                }
            }
        }

        public void Run(ExecFunc exec)
        {
            foreach (var func in Functions)
            {
                List<Param> refParams = new List<Param>();
                foreach (var refParam in func.ParamRefs)
                {
                    var paramVal = ScriptContext.GetRaw(refParam.Key);

                    refParams.Add(new Param
                    {
                        Name = refParam.Target,
                        Value = paramVal
                    });
                }
                func.Context.AddParams(refParams.ToArray());

                exec(func.Name, func.Context);

                if (func.OutputKey == "$out")
                {
                    OutputContext.AddParams(func.Context.GetResults());
                }
                if (func.OutputKey != null)
                {
                    ScriptContext.AddParams(func.Context.GetResults(func.OutputKey + "."));
                }
            }
        }
    }

    class Scripting
    {
        private List<Script> Scripts = new List<Script>();
        private bool NoReload { get; }

        private ExecFunc ExecDelegate { get; }

        public Scripting(ExecFunc executor, bool noReload = false)
        {
            ExecDelegate = executor;
            NoReload = noReload;
        }

        private static readonly string CWD = Directory.GetCurrentDirectory();

        private static string FormatScriptPath(string scriptPath)
        {
            if (scriptPath.StartsWith("./"))
            {
                scriptPath = Path.Combine(CWD, scriptPath.Substring(2));
            }
            if (scriptPath.StartsWith(@".\\"))
            {
                scriptPath = Path.Combine(CWD, scriptPath.Substring(3));
            }

            return scriptPath;
        }

        public void AddScript(string scriptPath)
        {
            scriptPath = FormatScriptPath(scriptPath);

            var fileInfo = new FileInfo(scriptPath);
            var fileName = fileInfo.Name;

            if (fileInfo.Exists == false)
            {
                throw new Exception(string.Format("script {0} cannot be found", scriptPath));
            }

            var script = new Script
            {
                Name = fileName.Substring(0, fileName.Length - fileInfo.Extension.Length),
                Filepath = fileInfo.FullName
            };

            script.Reload();
            Scripts.Add(script);
        }

        public Script FindScript(string name)
        {
            return Scripts.Find(s => s.Name == name);
        }

        public Context RunScript(Script script, Context context = null)
        {
            if (NoReload == false)
            {
                script.Reload();
            }

            if (context == null)
            {
                context = new Context();
            }

            ScriptExecutor executor = new ScriptExecutor(script.Lines, context);
            executor.Run(ExecDelegate);

            return context;
        }

        public void RunAllScripts()
        {
            Scripts.ForEach(script =>
            {
                RunScript(script);
            });
        }

    }
}
