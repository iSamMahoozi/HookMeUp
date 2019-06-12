using System;
using System.Runtime.Serialization;

namespace CustomBuildTasks
{
    // TODO: Allow Hook to throw exceptions ONLY by embedding them in a HookException and throwing that
    [Serializable]
    public sealed class HookingException : Exception
    {
        private HookingException() { }
        private HookingException(string message) : base(message) { }
        private HookingException(string message, Exception inner) : base(message, inner) { }
        private HookingException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public HookingException(Exception inner, string message = null) : base(message, inner) { }
    }
}