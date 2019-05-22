// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;
using SE.Config;

namespace SE.Forge.VisualStudio
{
    public class VisualCpp2015 : CppProjectGenerator, IConfigReceiver
    {
        private const string ToolDisplayName = "Visual Studio 2015";

        [AutoConfig("vs2015", Text = "Generates Visual Studio 2015 project files and solution")]
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

        static VisualCpp2015()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<VisualCpp2015>(true, ref result);
        }
        public VisualCpp2015()
            : base(VisualStudioUtils.GetEditorVersion(VisualStudioVersionFlag.VisualStudio2015))
        { }

        public override Task CreateInstance()
        {
            return new VisualCpp2015();
        }

        public void GetAdditionalManualInfo(PageFormatter page)
        { }
    }
}
