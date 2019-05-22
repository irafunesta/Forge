// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;
using SE.Config;

namespace SE.Forge.VisualStudio
{
    public class VisualStudio2017Solution : SolutionGenerator
    {
        [AutoConfig("vs2017")]
        private static bool enabled = false;
        public override bool Enabled
        {
            get { return (version != null && enabled); }
        }

        static VisualStudio2017Solution()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<VisualStudio2017Solution>(true, ref result);
        }
        public VisualStudio2017Solution()
            : base(VisualStudioUtils.GetEditorVersion(VisualStudioVersionFlag.VisualStudio2017))
        { }

        public override Task CreateInstance()
        {
            return new VisualStudio2017Solution();
        }
    }
}
