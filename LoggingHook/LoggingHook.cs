using System;
using HookMeUp;

namespace TestStaticAspectInjection
{
    internal class LoggingHook : Hook
    {
        public override void OnEnter(HookingContext context)
        {
#warning As a test we are simply replacing {{VALUE}} with Hello World
            Console.WriteLine("{{VALUE}}");
        }
    }
}
