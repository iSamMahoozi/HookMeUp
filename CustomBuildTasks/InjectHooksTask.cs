using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

namespace CustomBuildTasks
{
    public sealed class InjectHooksTask : Task
    {
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

        public override bool Execute()
        {
            var CRLF = Environment.NewLine;
            var compileFiles = Inputs.Split(';');
            foreach (var compileFile in compileFiles)
            {
                try
                {
                    if (compileFile.EndsWith(".g.cs"))
                    {
                        continue;
                    }
                    var content = File.ReadAllText(compileFile).Replace("{{VALUE}}", "Hello World!");

                    var generatedFile = Regex.Replace(compileFile, ".cs$", ".g.cs");
                    File.WriteAllText(generatedFile, "/* GENERATED */" + CRLF + content);
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Could not open {compileFile}. {ex}");
                }
            }

            Outputs = "*.g.cs";
            Log.LogMessage(MessageImportance.High, $"Items to compile:{CRLF}{string.Join(CRLF, compileFiles)}");
            return true;
        }
    }
}
