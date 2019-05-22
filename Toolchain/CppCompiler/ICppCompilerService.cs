// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;

namespace SE.Forge.CppCompiler
{
    public interface ICppCompilerService : ICompilerService
    {
        bool Enabled { get; }
    }
}
