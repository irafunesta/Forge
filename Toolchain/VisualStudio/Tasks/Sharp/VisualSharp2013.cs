// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;
using SE.Config;

namespace SE.Forge.VisualStudio
{
    public class VisualSharp2013 : SharpProjectGenerator, IConfigReceiver
    {
        private const string ToolDisplayName = "Visual Studio 2013";

        [AutoConfig("vs2013", Text = "Generates Visual Studio 2013 project files and solution")]
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

        static VisualSharp2013()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<VisualSharp2013>(true, ref result);
        }
        public VisualSharp2013()
            : base(VisualStudioUtils.GetEditorVersion(VisualStudioVersionFlag.VisualStudio2013))
        { }

        public override Task CreateInstance()
        {
            return new VisualSharp2013();
        }

        public void GetAdditionalManualInfo(PageFormatter page)
        { }
    }
}
