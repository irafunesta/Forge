// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Forge.Systems;
using SE.Config;

namespace SE.Forge.SharpCompiler
{
    public partial class CompilerTask : IConfigReceiver
    {
        private const string ToolDisplayName = "C#";

        [AutoConfig("csc", Text = "Compiles C# source into x64 assembly")]
        private static bool enabled = false;

        public string DisplayName
        {
            get { return ToolDisplayName; }
        }

        static CompilerTask()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<CompilerTask>(true, ref result);
        }

        public void GetAdditionalManualInfo(PageFormatter page)
        { }
    }
}
