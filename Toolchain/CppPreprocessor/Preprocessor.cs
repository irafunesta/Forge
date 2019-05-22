// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SE.Code.Parsing;
using SE.Code;
using SE.Text;

namespace SE.Forge.CppPreprocessor
{
    /**
     ISO Standard 2017-03-21
     http://www.open-std.org/jtc1/sc22/wg21/docs/papers/2017/n4659.pdf
    */

    public static partial class Preprocessor
    {
        public readonly static UInt32 DefinedKeyword = "defined".Fnv32();
        public readonly static UInt32 VariadicKeyword = VariadicArgs.Fnv32();
        public const string VariadicArgs = "__VA_ARGS__";

        public static bool ThrowError(CodeProcessorContext context, int errorCode)
        {
            if (context.Callback != null)
            {
                context.States.Push(errorCode);
                return context.Callback(context, null, null);
            }
            else return false;
        }
        public static bool ThrowError(CodeProcessorContext context, Stream data)
        {
            if (context.Callback != null)
            {
                context.States.Push(PreprocessorCodes.Error);
                return context.Callback(context, data, null);
            }
            else return false;
        }

        public static bool SimpleWhitespace(Stream data)
        {
            return (data.Skip(" \t\r\n") > 0);
        }
        public static bool LineBreak(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            char c = data.Peek();
            if (c == 0 || c == '\n')
            {
                context.IncreaseLineNumber(data);

                if (c != 0)
                    data.Get();
                return true;
            }
            else return false;
        }
        public static bool LineBreak(Stream data)
        {
            char c = data.Peek();
            return (c == 0 || c == '\n');
        }
        public static bool EscapedLineBreak(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (data.Peek() != '\\')
                return false;

            long streamPos = data.Position;
            data.Get();

            data.Skip(" \t\r");

            if (LineBreak(context, data, nodes)) return true;
            else
            {
                data.Position = streamPos;
                return false;
            }
        }
        public static bool EscapedLineBreak(CodeProcessorContext context, Stream data)
        {
            if (data.Peek() != '\\')
                return false;

            long streamPos = data.Position;
            data.Get();

            data.Skip(" \t\r");

            TokenStream nodes = new TokenStream();
            if (LineBreak(context, data, nodes)) return true;
            else
            {
                data.Position = streamPos;
                return false;
            }
        }
        public static bool Whitespace(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            long streamPos = data.Position;
            do
            {
                data.Skip(" \t\r");
            }
            while (EscapedLineBreak(context, data, nodes));
            return ((data.Position - streamPos) != 0);
        }

        public static bool SingleLineComment(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (data.Peek() != '/')
                return false;

            data.Get();
            while (!data.Eof())
            {
                if (!EscapedLineBreak(context, data, nodes))
                {
                    if (!LineBreak(data))
                        data.Get();
                    else
                        break;
                }
            }
            return true;
        }
        public static bool MultiLineComment(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (data.Peek() != '*')
                return false;

            data.Get();
            while (!data.Eof())
            {
                if (!LineBreak(context, data, nodes) && data.Get() == '*' && data.Peek() == '/')
                {
                    data.Get();
                    return true;
                }
            }
            return false;
        }
        public static bool Comment(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (data.Peek() != '/')
                return false;
            
            long streamPos = data.Position;
            data.Get();

            if (SingleLineComment(context, data, nodes) ||
                MultiLineComment(context, data, nodes))
            {
                long length = data.Position - streamPos;
                data.Position -= length;

                for (; length > 0; length--)
                    nodes.Add(data.Get());
                return true;
            }

            data.Position = streamPos;
            return false;
        }
        public static bool Comment(CodeProcessorContext context, Stream data)
        {
            if (data.Peek() != '/')
                return false;

            long streamPos = data.Position;
            data.Get();

            TokenStream nodes = new TokenStream();
            if (SingleLineComment(context, data, nodes) ||
                MultiLineComment(context, data, nodes))
            {
                return true;
            }

            data.Position = streamPos;
            return false;
        }

        public static bool String(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            char c = data.Peek();
            if (c != '\'' && c != '\"')
                return false;

            long streamPos = data.Position;
            data.Get();

            while (!data.Eof())
            {
                if (data.Peek() == '\\')
                {
                    data.Get();
                    if (data.Peek() == c)
                        data.Get();
                    else
                        data.Position--;
                }
                if (!EscapedLineBreak(context, data, nodes))
                {
                    if (data.Get() == c)
                    {
                        long length = data.Position - streamPos;
                        data.Position -= length;

                        for (; length > 0; length--)
                            nodes.Add(data.Get());
                        return true;
                    }
                    else if (LineBreak(context, data, nodes))
                        break;
                }
            }

            data.Position = streamPos;
            return false;
        }
        public static bool Identifier(Stream data, out IdentifierToken token)
        {
            token = new IdentifierToken();

            char c = data.Peek();
            if (c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                long streamPos = data.Position;
                long length = data.SkipLiteral("_", true);
                if (length > 0)
                {
                    data.Position = streamPos;
                    token.Id = data.Read(length).Fnv32();
                    token.Length = length;

                    data.Position = streamPos;
                    return true;
                }
            }
            return false;
        }
    }
}
