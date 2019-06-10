using System;

namespace TestStaticAspectInjection
{
    internal class Program
    {
        private static void Main()
        {
#warning Breakpoints will never be hit since we are using a different file. We should map the generated source files to the original ones and simply skip the generated lines (if possible)
            new LoggingHook().OnEntry();
            Console.ReadLine();
        }
    }
}
