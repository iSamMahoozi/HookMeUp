using System;

namespace TestStaticAspectInjection
{
    internal class Program
    {
        private static void Main()
        {
#warning Breakpoints will never be hit since we are using a different file. We should map the generated source files to the original ones and simply skip the generated lines (if possible)
#warning Breakpoints can be hit if we rewrite the symbols' document url - However this doesn't take into consideration changes in the number of lines in the document (yet)
            Console.WriteLine("Attach Debugger");
            Console.ReadLine();

            new LoggingHook().OnEntry();
            Console.ReadLine();
        }
    }
}
