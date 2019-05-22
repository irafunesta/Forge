// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;
using SE.Config;

namespace SE.Forge.VisualStudio
{
    public class VisualCpp2017 : CppProjectGenerator, IConfigReceiver
    {
        private const string ToolDisplayName = "Visual Studio 2017";

        [AutoConfig("vs2017", Text = "Generates Visual Studio 2017 project files and solution")]
        private static bool enabled = false;
        public override bool Enabled
        {
            get { return (version != null && enabled); }
        }
        public bool Display
        {
            get { return (version != null); }
        }

        public string DisplayName
        {
            get { return ToolDisplayName; }
        }

        static VisualCpp2017()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<VisualCpp2017>(true, ref result);
        }
        public VisualCpp2017()
            : base(VisualStudioUtils.GetEditorVersion(VisualStudioVersionFlag.VisualStudio2017))
        { }

        public override Task CreateInstance()
        {
            return new VisualCpp2017();
        }

        public void GetAdditionalManualInfo(PageFormatter page)
        { }
    }
}
