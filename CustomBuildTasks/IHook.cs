using System;

namespace CustomBuildTasks
{
    // TODO: Add async alternatives (should they only be allowed within async HookPoints?)
    public interface IHook
    {
        /// <summary>
        /// Returns whether or not an available <see cref="HookPoint"/> should be hooked
        /// </summary>
        /// <param name="point">Represents information about the <see cref="HookPoint"/> in the code that can be hooked</param>
        /// <returns>whether or not the <paramref name="point"/> should be hooked. The results of this evaluation may be cached.</returns>
        bool ShouldHook(HookPoint point);

        /// <summary>
        /// This method is called when a <see cref="HookingContext"/> has been entered
        /// </summary>
        /// <param name="context">Provides context of the <see cref="HookPoint"/>, passed arguments, and hooking flow control</param>
        void OnEnter(HookingContext context);

        /// <summary>
        /// This method is called before a <see cref="HookingContext"/> exits
        /// </summary>
        /// <param name="context">Provides context of the <see cref="HookPoint"/>, passed arguments, and hooking flow control</param>
        void OnExit(HookingContext context);

        /// <summary>
        /// This method is called when an <see cref="Exception"/> is thrown
        /// </summary>
        /// <exception cref="HookingException">In order to throw exceptions into the original code, the exception MUST be wrapped in a <see cref="HookingException"/></exception>
        /// <param name="context">Provides context of the <see cref="HookPoint"/>, passed arguments, and hooking flow control</param>
        /// <param name="ex">The exception thrown by the original code</param>
        /// <param name="isCaught">Whether or not <paramref name="ex"/> was caught by the original code</param>
        /// <returns> whether or not <paramref name="ex"/> has been handled. Returning true will allow the handler to swallow <paramref name="ex"/></returns>
        bool HandleException(HookingContext context, Exception ex, bool isCaught);
    }
}
