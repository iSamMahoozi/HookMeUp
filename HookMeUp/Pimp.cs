using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HookMeUp {
	public class Pimp {
		[ImportMany]
		private static IEnumerable<IHook> Hooks;

		static Pimp() {
			var exeLocation = Assembly.GetEntryAssembly().Location;
			var path = Path.Combine(Path.GetDirectoryName(exeLocation), "plugins");

			var catalog = new AggregateCatalog();
			catalog.Catalogs.Add(new DirectoryCatalog(path));

			var _container = new CompositionContainer(catalog);

			try {
				Hooks = _container.GetExportedValues<IHook>();
			} catch (Exception ex) {
				Debug.WriteLine(ex);
			}
		}

		public static void OnEnter(HookingContext context) {
			if (Hooks != null) {
				foreach (var hook in Hooks.Where(x => x.ShouldHook(context.HookPoint))) {
					hook.OnEnter(context);
				}
			}
		}

		public static void OnExit(HookingContext context) {
			if (Hooks != null) {
				foreach (var hook in Hooks.Where(x => x.ShouldHook(context.HookPoint))) {
					hook.OnExit(context);
				}
			}
		}

		public static void OnException(HookingContext context, Exception ex) {
			if (Hooks != null) {
				foreach (var hook in Hooks.Where(x => x.ShouldHook(context.HookPoint))) {
					hook.HandleException(context, ex, false);
				}
			}
		}
	}
}
