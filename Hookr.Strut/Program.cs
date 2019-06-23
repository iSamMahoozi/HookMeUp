using System;
using System.IO;

namespace Hookr.Strut {
	internal class Program {
		private static void Main(string[] args) {
#if DEBUG
			Console.WriteLine("Attach debgger now");
			Console.ReadLine();
#endif
			if (args.Length > 0 && File.Exists(args[0])) {
				var fileContent = File.ReadAllText(args[0]);
				using (var strut = new Strutter(fileContent)) {
					strut
						.InjectIntoMethods()
						.ReplaceMethods()
						.WriteAllToFile(args[0]);
				}

			}
		}
	}
}
