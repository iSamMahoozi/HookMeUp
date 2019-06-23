using System.Reflection;

namespace HookMeUp {
	// TODO: This class needs to provide the actual "static" HookPoint, as well as information about the current context (ex: the instance, arguments if in a method, etc.), in addition to Hook Flow Control
	public class HookingContext {
		public HookingContext(object instance, MethodBase method) {

		}

		public HookingContext WithReturnValue(object returnValue) {
			ReturnValue = returnValue;
			return this;
		}

		public object ReturnValue { get; private set; }
		public HookPoint HookPoint { get; }
		public HookFlow Flow { get; }
	}
}