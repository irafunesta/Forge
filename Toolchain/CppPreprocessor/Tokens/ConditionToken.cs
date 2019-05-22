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
    public class ConditionToken : PreprocessorToken
    {
        protected TokenStream condition = new TokenStream();
        public TokenStream Condition
        {
            get { return condition; }
        }

        public ConditionToken(TokenTypes type)
            : base(type)
        { }

        public override void Deserialize(Stream stream)
        {
            base.Deserialize(stream);

            switch ((TokenTypes)type)
            {
                case TokenTypes.IfDefined:
                case TokenTypes.IfNotDefined:
                    {
                        IdentifierToken token; if (Preprocessor.Identifier(stream, out token))
                        {
                            stream.Position += token.Length;
                            condition.Encode(token.Id);
                        }
                    }
                    break;
                case TokenTypes.ElseIf:
                case TokenTypes.If:
                    {
                        CodeProcessorContext context = new CodeProcessorContext(new CodeProcessorConfig());
                        Preprocessor.Any(context, stream, condition);
                    }
                    break;
            }

            if ((TokenTypes)stream.Peek() != TokenTypes.TokenSeparator)
                throw new InvalidDataException();

            stream.Get();
        }
    }
}
