// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.VisualStudio
{
    public enum VisualStudioVersionFlag : byte
    {
        Invalid = 0,

        VisualStudio2017, //15.0
        VisualStudio2015, //14.0
        VisualStudio2013, //12.0
        VisualStudio2012, //11.0
        VisualStudio2010, //10.0
    }
}
