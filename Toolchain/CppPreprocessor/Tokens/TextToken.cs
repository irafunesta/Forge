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
    public class TextToken : PreprocessorToken
    {
        protected TokenStream text = new TokenStream();
        public TokenStream Text
        {
            get { return text; }
        }

        public TextToken(TokenTypes type = TokenTypes.Text)
            : base(type)
        { }

        public override void Deserialize(Stream stream)
        {
            base.Deserialize(stream);

            CodeProcessorContext context = new CodeProcessorContext(new CodeProcessorConfig());
            Preprocessor.Any(context, stream, text);

            if ((TokenTypes)stream.Peek() != TokenTypes.TokenSeparator)
                throw new InvalidDataException();

            stream.Get();
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", ((TokenTypes)type).ToString(), text.ToString());
        }
    }
}
