using System;
using System.ComponentModel.Composition;
using HookMeUp;

namespace TestStaticAspectInjection {
	[Export(typeof(IHook))]
	internal class LoggingHook : Hook {
		public override void OnEnter(HookingContext context) {
			Console.WriteLine($"enter {context.HookPoint.Method}");
		}
	}
}
