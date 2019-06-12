using System;
using CustomBuildTasks;

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
