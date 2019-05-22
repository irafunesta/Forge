// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Code.Build;

namespace SE.Forge.Systems
{
    public interface ICompilerService
    {
        TargetPlatform[] Platforms { get; }
        TargetArchitecture[] Architectures { get; }

        BuildMode[] GetBuildModes(IEnumerable<string> parameter);
    }
}
