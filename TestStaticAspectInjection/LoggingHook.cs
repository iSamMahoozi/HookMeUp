using CustomBuildTasks;

namespace TestStaticAspectInjection
{
    internal class LoggingHook : Hook
    {
        public override void OnEntry()
        {
#warning As a test we are simply replacing {{VALUE}} with Hello World
            System.Console.WriteLine("{{VALUE}}");
        }
    }
}
