using System;

namespace CLI
{
    public class Command
    {
        public string Name { get; set; }
        [Obsolete("Namespace is deprecated, use Name instead")]
        public string Namespace
        {
            get
            {
                return Name;
            }
            set
            {
                Name = value;
            }
        }
        public Action<Context> Handler { get; set; }
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

        public bool Run(Context context)
        {
            try
            {
                if (Handler == null)
                {
                    Console.WriteLine("Command \"{0}\" has no action!", Name);
                    return false;
                }

                Handler.Invoke(context);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Command \"{0}\" returned with an exception: {1}", Name, ex.Message);
                Console.WriteLine("Stacktrace {0}", ex.StackTrace);
            }

            return false;
        }

    }
}
