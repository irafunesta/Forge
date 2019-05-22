// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;
using SE.Code.Build;
using SE.Config;

namespace SE.Forge.Msvc
{
    public partial class CppCompiler : IConfigReceiver
    {
        private const string ToolDisplayName = "MSVC";

        [AutoConfig("msvc", Text = "Compiles C++ source using MSVC compiler")]
        private static bool enabled = false;

        public string DisplayName
        {
            get { return ToolDisplayName; }
        }
        public bool Display
        {
            get { return false; }
        }

        static CppCompiler()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<CppCompiler>(true, ref result);
        }

        public void GetAdditionalManualInfo(PageFormatter page)
        {
            foreach (TargetArchitecture architecture in Architectures)
                foreach (TargetPlatform platform in Platforms)
                        page.AddRow(string.Format("-{0}", platform.GetTechnicalName(architecture)), string.Format("Enables {0} {1} platform handling", architecture, platform.Name));
        }
    }
}
