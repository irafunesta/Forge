// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Text;

namespace SE.Forge.Systems
{
    public static class SystemTags
    {
        //General
        public readonly static UInt32 Analytics = "Analytics".Fnv32();

        //Tasks
        public readonly static UInt32 ForgeSetup = "Setup".Fnv32();
        public readonly static UInt32 SealedSetup = "SealedSetup".Fnv32();

        //Build
        public readonly static UInt32 SharpSetup = "C#Setup".Fnv32();
        public readonly static UInt32 SharpModule = "C#Module".Fnv32();
        public readonly static UInt32 Sharp = "C#".Fnv32();

        public readonly static UInt32 CppSetup = "C++Setup".Fnv32();
        public readonly static UInt32 Cpp = "C++".Fnv32();
    }
}
