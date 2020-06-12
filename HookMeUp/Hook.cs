using System;

namespace HookMeUp {
	public abstract class Hook : IHook {
		public virtual void OnEnter(HookingContext context) { }
		public virtual void OnExit(HookingContext context) { }
		public virtual bool HandleException(HookingContext context, Exception ex, bool isCaught) {
			return isCaught;
		}

		public virtual bool ShouldHook(HookPoint point) => true;
	}
}
