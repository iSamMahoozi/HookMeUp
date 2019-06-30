using System;

namespace TestStaticAspectInjection {
	internal class Program {
		private static void Main() {
			Console.WriteLine("Application is running...");
			if (Test() == null) {
				Console.WriteLine(Sum(1, 2));
			}

			Console.ReadLine();

		}

		public static int Sum(int x, int y) {
			var z = x + y;
			return z;
		}

		protected static IDisposable Test() => (IDisposable)null;
	}
}
