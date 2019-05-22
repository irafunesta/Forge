// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Config;

namespace SE.Forge.SharpAnalyzer
{
    public partial class Analyzer
    {
        static Analyzer()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<Analyzer>(true, ref result);
        }


    }
}
