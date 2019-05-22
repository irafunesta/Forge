// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.CppPreprocessor
{
    public static class PreprocessorCodes
    {
        public readonly static int Config = 0;

        public readonly static int IncludeHeader = 0;
        public readonly static int IncludeSource = 0;

        public readonly static int Error = 0;

        public readonly static int AlreadyDefined = 0;
        public readonly static int MissingBracket = 0;
        public readonly static int UnexpectedCharacter = 0;
        public readonly static int VariadicMacroInParameterList = 0;
        public readonly static int VariadicParameterMismatch = 0;
        public readonly static int ReservedKeyword = 0;
        public readonly static int NotDefined = 0;
        public readonly static int IdentifierExpected = 0;
        public readonly static int ElseWithoutIf = 0;
        public readonly static int EndWithoutIf = 0;
        public readonly static int ParameterExpected = 0;
        public readonly static int StartOfFile = 0;
        public readonly static int EndOfFile = 0;
        public readonly static int ParameterMismatch = 0;
        public readonly static int ExpressionExpected = 0;
        public readonly static int InvalidToken = 0;
    }
}
