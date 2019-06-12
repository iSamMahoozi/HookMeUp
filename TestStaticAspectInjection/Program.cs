using System;

namespace TestStaticAspectInjection
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("Attach Debugger");
            Console.ReadLine();

            new LoggingHook().OnEnter(null);
            Console.ReadLine();
        }
    }
}
