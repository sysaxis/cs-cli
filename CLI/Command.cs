using System;
using System.Threading.Tasks;

namespace CLI
{
    public class Command
    {
        public string Name { get; set; }

        private Action<Context> action = null;
        private Func<Context, Task> asyncAction = null;

        public Action<Context> Handler
        {
            get
            {
                return action;
            }
            set
            {
                if (asyncAction != null)
                {
                    throw new Exception("Command handler has already been defined");
                }

                action = value;
            }
        }

        public Func<Context, Task> AsyncHandler
        {
            get
            {
                return asyncAction;
            }
            set
            {
                if (action != null)
                {
                    throw new Exception("Command handler has already beed defined");
                }

                asyncAction = value;
            }
        }


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
                if (Handler != null) Handler.Invoke(context);
                else if (AsyncHandler != null) AsyncHandler.Invoke(context).Wait();
                else
                {
                    Console.WriteLine("Command \"{0}\" has no action!", Name);
                    return false;
                }

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
