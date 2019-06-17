using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Hookr {
	public sealed class HookTask : Task {
		[Required]
#warning Can we handle Wildcards or are they automatically expanded?
#warning Generated files, if added to the project should be nested under the originals
#warning During testing initial build doesn't work because the injecting task assembly hasn't been created yet so build fails
#warning During testing multiple builds may not work because MSBuild is accessing the previous assembly
#warning The UsingTask MSBuild element refers to the AssemblyFile rather than just the name
#warning Hooks should be excluded from hooking process
		public string Inputs { get; set; }

		[Output]
		public string Outputs { get; set; }

		[Required]
		public string HookrStrut { get; set; }

		public override bool Execute() {
			// System.Diagnostics.Debugger.Launch();
			var CRLF = Environment.NewLine;
			var compileFiles = Inputs.Split(';');
			foreach (var compileFile in compileFiles) {
				try {
					if (compileFile.EndsWith(".g.cs")) {
						continue;
					}

					var originalContent = File.ReadAllText(compileFile);
					var psi = new ProcessStartInfo {
						CreateNoWindow = false,
						FileName = HookrStrut,
						Arguments = $"\"{compileFile}\"",
						LoadUserProfile = false,
						UseShellExecute = true,
						WindowStyle = ProcessWindowStyle.Normal,
						RedirectStandardError = false,
						RedirectStandardOutput = false,
					};
					var strut = new Process {
						StartInfo = psi,
					};
					strut.Start();
					strut.WaitForExit();
				} catch (Exception ex) {
					Log.LogWarning($"Could not open {compileFile}. {ex}");
				}
			}

			Outputs = "*.g.cs";
			Log.LogMessage(MessageImportance.High, $"Items to compile:{CRLF}{string.Join(CRLF, compileFiles)}");
			return true;
		}
	}
}