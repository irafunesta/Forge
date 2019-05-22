// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using SE.Setup;

namespace Setup
{
    public class Forge : IInstaller
    {
        const string ForgePath = "Deploy/Tools/Forge.exe";
        const string ForgeSource = "Tools/Forge";

        private static readonly string[] ForgeModules = new string[]
            {
                "Tools/Modules/App",
                "Tools/Modules/Code",
                "Tools/Modules/Config",
                "Tools/Modules/Crypto",
                "Tools/Modules/Encode",
                "Tools/Modules/Json",
                "Tools/Modules/Mixins",
                "Tools/Modules/Parallel",
                "Tools/Modules/Reflection",
                "Tools/Modules/Shell",
                "Tools/Modules/Storage",
                "Tools/Modules/Text",
                "Tools/Modules/Threading",
                "Tools/Modules/Types"
            };

        public Forge()
        { }

        public void Execute()
        {
            Compiler compiler = new Compiler();
            compiler.Options.Dependencies.Add("System.dll");
            compiler.Options.Dependencies.Add("System.Core.dll");
            compiler.Options.Dependencies.Add("System.Data.dll");
            compiler.Options.Dependencies.Add("System.XML.dll");
            compiler.Options.Dependencies.Add("Microsoft.CSharp.dll");

            compiler.Options.TargetFile = Path.Combine(SdkSetup.SdkPath, ForgePath);
            SdkSetup.SetFiles(compiler.Options, ForgeSource, ".cs", ".Build.cs", ".Setup.cs");

            foreach(string module in ForgeModules)
                SdkSetup.SetFiles(compiler.Options, Path.Combine(SdkSetup.SdkPath, module), ".cs", ".Build.cs", ".Setup.cs");

            compiler.Options.OutputType = CompilerOutputType.Console;
            compiler.Options.Debug = false;

            compiler.Execute();
        }
    }
}
