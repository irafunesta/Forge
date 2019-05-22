// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Linq;
using Microsoft.CSharp;
using SE.App;

namespace SE.Forge.SharpCompiler
{
    public class Compiler
    {
        private static Dictionary<string, string> config = new Dictionary<string, string>();

        CompilerOptions options = new CompilerOptions();
        public CompilerOptions Options
        {
            get { return options; }
        }

        static Compiler()
        {
            config.Add("CompilerVersion", "v4.0");
        }
        public Compiler()
        { }

        public void Execute()
		{
			CompilerResults result = new CSharpCodeProvider(config).CompileAssemblyFromFile(options.Finalize(), options.Sources.Cast<string>().ToArray());
			if (result.Errors.Count > 0)
			{
				foreach (CompilerError e in result.Errors)
				{
                    if (e.IsWarning) Application.Warning(SeverityFlags.Minimal, e.ToString());
                    else Application.Error(SeverityFlags.Minimal, e.ToString());
				}
				if (result.Errors.HasErrors || options.WarningAsError)
					throw new Exception();
			}
			options.TemporaryFiles.Delete();
		}
    }
}
