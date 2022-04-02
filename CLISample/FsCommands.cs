using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using CLI;

namespace CLISample
{
    class FsCommands
    {
        public FsCommands()
        {
            new Command
            {
                Name = "cp",
                Handler = cnx =>
                {
                    File.Copy(cnx.Get("from", "f"), cnx.Get("to", "t"));
                }
            };
        }
    }
}
