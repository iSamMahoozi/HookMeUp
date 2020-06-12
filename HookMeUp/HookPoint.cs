using System.Reflection;

namespace HookMeUp {
	// TODO: This class should describe exactly where the hooking can/will occur, including: filename, line number, namespace, class information, method/property/etc. information, and where in the method/property/etc. (ex: OnEnter, OnExit, etc.)
	public class HookPoint {
		public HookPoint(object instance, MethodBase method, string memberName, string filePath, int lineNumber) {
			Instance = instance;
			Method = method;

			MemberName = memberName;
			FilePath = filePath;
			LineNumber = lineNumber;
		}

		public HookType HookType { get; set; }

		public object Instance { get; }
		public MethodBase Method { get; }
		public string MemberName { get; }
		public string FilePath { get; }
		public int LineNumber { get; }
	}
}