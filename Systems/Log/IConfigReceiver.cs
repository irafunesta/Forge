// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Config;

namespace SE.Forge.Systems
{
    public interface IConfigReceiver
    {
        string DisplayName { get; }
        bool Display { get; }

        void GetAdditionalManualInfo(PageFormatter page);
    }
}
