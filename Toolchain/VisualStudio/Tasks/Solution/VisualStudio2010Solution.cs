// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;
using SE.Config;

namespace SE.Forge.VisualStudio
{
    public class VisualStudio2010Solution : SolutionGenerator
    {
        [AutoConfig("vs2010")]
        private static bool enabled = false;
        public override bool Enabled
        {
            get { return (version != null && enabled); }
        }

        static VisualStudio2010Solution()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<VisualStudio2010Solution>(true, ref result);
        }
        public VisualStudio2010Solution()
            : base(VisualStudioUtils.GetEditorVersion(VisualStudioVersionFlag.VisualStudio2010))
        { }

        public override Task CreateInstance()
        {
            return new VisualStudio2010Solution();
        }
    }
}
