// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.CppCompiler;
using SE.Code.Build;
using SE.Text;

namespace SE.Forge.Msvc
{
    public partial class CppCompiler : ICppCompilerService
    {
        class WindowsTargetPlatform : TargetPlatform
        {
            private const string PlatformName = "Windows";
            private const string WindowsX86 = "Win32";
            private const string WindowsX64 = "Win64";

            public override string Name
            {
                get { return PlatformName; }
            }

            public WindowsTargetPlatform()
            {
                definitions.Add("__cplusplus", "199711L");
            }

            public override string GetTechnicalName(TargetArchitecture architecture)
            {
                switch (architecture)
                {
                    case TargetArchitecture.Default:
                    case TargetArchitecture.x64:
                        return WindowsX64;
                    case TargetArchitecture.x86:
                        return WindowsX86;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
            public override void SetDependentDefines(TargetArchitecture architecture)
            {
                switch (architecture)
                {
                    case TargetArchitecture.x86:
                        definitions.Add("_WIN32", "1");
                        break;
                    case TargetArchitecture.x64:
                        definitions.Add("_WIN32", "1");
                        definitions.Add("_WIN64", "1");
                        break;
                }
            }
        };
        private readonly static TargetPlatform[] TargetPlatforms = new TargetPlatform[]
            {
                new WindowsTargetPlatform()
            };
        private readonly static TargetArchitecture[] TargetArchitectures = new TargetArchitecture[]
            {
                TargetArchitecture.x86,
                TargetArchitecture.x64
            };

        public TargetPlatform[] Platforms
        {
            get { return TargetPlatforms; }
        }
        public TargetArchitecture[] Architectures
        {
            get { return TargetArchitectures; }
        }

        public bool Enabled
        {
            get { return enabled; }
        }

        public CppCompiler()
        { }

        public BuildMode[] GetBuildModes(IEnumerable<string> parameter)
        {
            List<BuildMode> modes = new List<BuildMode>();
            modes.Add(new BuildMode("Debug", true, false, false));

            return modes.ToArray();
        }
    }
}
