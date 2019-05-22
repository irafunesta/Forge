// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;
using SE.Config;

namespace SE.Forge.VisualStudio
{
    public class VisualSharp2012 : SharpProjectGenerator, IConfigReceiver
    {
        private const string ToolDisplayName = "Visual Studio 2012";

        [AutoConfig("vs2012", Text = "Generates Visual Studio 2010 project files and solution")]
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

        static VisualSharp2012()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<VisualSharp2012>(true, ref result);
        }
        public VisualSharp2012()
            : base(VisualStudioUtils.GetEditorVersion(VisualStudioVersionFlag.VisualStudio2012))
        { }

        public override Task CreateInstance()
        {
            return new VisualSharp2012();
        }

        public void GetAdditionalManualInfo(PageFormatter page)
        { }
    }
}
