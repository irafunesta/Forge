// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Config;

namespace SE.Forge.Systems.Profiles
{
    public partial class LocalProfile
    {
        public const string PrintToolStackParameter = "PrintToolStack";

        [AutoConfig(PrintToolStackParameter, Text = "Outputs profiling info from the tool stack")]
        private static bool printToolStack = false;
    }
}
