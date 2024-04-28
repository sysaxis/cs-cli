using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

using CLI;
using static CLI.ConsoleUtil;

namespace CLISample
{
    class FsCommands
    {
        private string currentPath = Directory.GetCurrentDirectory();

        private string GetPath(string relpath)
        {
            if (relpath == null) return currentPath;

            string path = currentPath;
            if (relpath.StartsWith("/")) path = relpath;
            else if (relpath.Length > 0) path = Path.GetFullPath(Path.Combine(path, relpath));

            return path;
        }

        public FsCommands()
        {
            new Command
            {
                Name = "pwd",
                Handler = cnx =>
                {
                    Console.WriteLine(GetPath(null));
                }
            };

            new Command
            {
                Name = "cd",
                Handler = cnx =>
                {
                    var newPath = GetPath(cnx.Get("#1"));
                    if (Directory.Exists(newPath))
                    {
                        currentPath = newPath;
                    }
                    else
                    {
                        Console.WriteLine("path does not exist!");
                    }
                }
            };

            new Command
            {
                Name = "cp",
                Handler = cnx =>
                {
                    // for the sake of example we use named parameters
                    File.Copy(cnx.Get("from", "f"), cnx.Get("to", "t"));
                }
            };

            new Command
            {
                Name = "mv",
                Handler = cnx =>
                {
                    File.Move(cnx.Get("#1"), cnx.Get("#2"));
                }
            };

            new Command
            {
                Name = "ls",
                Handler = cnx =>
                {
                    string path = GetPath(cnx.Get("#1"));

                    var dirInfo = Directory.EnumerateFileSystemEntries(path).Select(p => new FileInfo(p)).ToList();

                    PrintList(dirInfo,
                        "CreationTime->Created(-20:yyyy-MM-dd HH:mm:ss)",
                        "Name(-50)",
                        "Attributes->Attrs");
                }
            };

            new Command
            {
                Name = "mkdir",
                Handler = cnx =>
                {
                    string relpath = cnx.Get("#1");
                    if (relpath == null)
                    {
                        relpath = PromptInput("dir name: ");
                    }

                    Directory.CreateDirectory(GetPath(relpath));
                }
            };
        }
    }
}
