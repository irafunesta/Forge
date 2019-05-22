// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.CppPreprocessor
{
    public enum TokenTypes : byte
    {
        TokenSeparator = 0,

        If = 1,
        IfDefined = 2,
        IfNotDefined = 3,
        ElseIf = 4,
        Else = 5,

        Define = 6,
        Undefine = 7,

        Error = 8,

        Include = 9,
        Pragma = 10,

        Text = 12
    }
}
