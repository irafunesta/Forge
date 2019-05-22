// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using SE.Code.Parsing;
using SE.Code;
using SE.Text;

namespace SE.Forge.CppPreprocessor.Tokens
{
    public class PreprocessorToken : Token
    {
        public PreprocessorToken(TokenTypes type)
            : base((byte)type)
        { }
        
        public override T GetValue<T>()
        {
            throw new NotImplementedException();
        }

        public override void Serialize(Stream stream)
        {
            throw new NotImplementedException();
        }
        public override void Deserialize(Stream stream)
        {
            if (stream.Peek() != type)
                throw new InvalidDataException();

            stream.Get();

            int bytes;
            line = stream.DecodeUInt32(out bytes);
            column = stream.DecodeUInt32(out bytes);

            if ((TokenTypes)stream.Peek() != TokenTypes.TokenSeparator)
                throw new InvalidDataException();

            stream.Get();
        }
    }
}
