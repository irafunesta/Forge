// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using SE.Code.Parsing;
using SE.Code;
using SE.Text;

namespace SE.Forge.CppPreprocessor
{
    public static class PreprocessorTokenizer
    {
        private static void CreateToken(long streamPos, UInt32 line, UInt32 column, long length, Stream data, TokenStream nodes, TokenTypes type)
        {
            long tmp = data.Position;
            data.Position = streamPos;

            nodes.Add(type);
            nodes.Encode(line);
            nodes.Encode(column);
            nodes.Add(TokenTypes.TokenSeparator);

            data.CopyRange(nodes, (int)length);

            nodes.Add(TokenTypes.TokenSeparator);
            data.Position = tmp;
        }
        private static void CreateToken(CodeProcessorContext context, Stream data, TokenStream nodes, TokenTypes type)
        {
            nodes.Add(type);
            nodes.Encode((UInt32)context.LineNumber);
            nodes.Encode((UInt32)(data.Position - context.LinePosition));
            nodes.Add(TokenTypes.TokenSeparator);

            Process(context, data, nodes);

            nodes.Add(TokenTypes.TokenSeparator);
        }

        private static bool Define(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            IdentifierToken token; if (Preprocessor.Identifier(data, out token))
            {
                if (token.Id == Preprocessor.DefinedKeyword || token.Id == Preprocessor.VariadicKeyword)
                    return Preprocessor.ThrowError(context, PreprocessorCodes.ReservedKeyword);

                data.Position--;
                CreateToken(context, data, nodes, TokenTypes.Define);
                return true;
            }
            return Preprocessor.ThrowError(context, PreprocessorCodes.IdentifierExpected);
        }
        private static bool Undefine(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            UInt32 line = (UInt32)context.LineNumber;
            UInt32 column = (UInt32)(data.Position - context.LinePosition);

            IdentifierToken token; if (Preprocessor.Identifier(data, out token))
            {
                if (token.Id == Preprocessor.DefinedKeyword || token.Id == Preprocessor.VariadicKeyword)
                    return Preprocessor.ThrowError(context, PreprocessorCodes.ReservedKeyword);

                long streamPos = data.Position - 1;
                data.Position += token.Length;

                while (!Preprocessor.LineBreak(data))
                {
                    Preprocessor.Whitespace(context, data, nodes);
                    if (!Preprocessor.Comment(context, data))
                        break;
                }

                if (Preprocessor.LineBreak(context, data, nodes))
                {
                    CreateToken(streamPos, line, column, token.Length, data, nodes, TokenTypes.Undefine);
                    return true;
                }
            }
            return Preprocessor.ThrowError(context, PreprocessorCodes.IdentifierExpected);
        }

        private static bool Include(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            data.Position--;
            CreateToken(context, data, nodes, TokenTypes.Include);
            return true;
        }

        private static bool Error(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            data.Position--;
            CreateToken(context, data, nodes, TokenTypes.Error);
            return true;
        }
        private static bool Pragma(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            data.Position--;
            CreateToken(context, data, nodes, TokenTypes.Pragma);
            return true;
        }

        private static bool Ifdef(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            UInt32 line = (UInt32)context.LineNumber;
            UInt32 column = (UInt32)(data.Position - context.LinePosition);

            IdentifierToken token; if (Preprocessor.Identifier(data, out token))
            {
                if (token.Id == Preprocessor.DefinedKeyword || token.Id == Preprocessor.VariadicKeyword)
                    return Preprocessor.ThrowError(context, PreprocessorCodes.ReservedKeyword);

                long streamPos = data.Position;
                data.Position += token.Length;

                while (!Preprocessor.LineBreak(data))
                {
                    Preprocessor.Whitespace(context, data, nodes);
                    if (!Preprocessor.Comment(context, data))
                        break;
                }

                if (Preprocessor.LineBreak(context, data, nodes))
                {
                    CreateToken(streamPos, line, column, token.Length, data, nodes, TokenTypes.IfDefined);
                    return true;
                }
            }
            return Preprocessor.ThrowError(context, PreprocessorCodes.IdentifierExpected);
        }
        private static bool Ifndef(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            UInt32 line = (UInt32)context.LineNumber;
            UInt32 column = (UInt32)(data.Position - context.LinePosition);

            IdentifierToken token; if (Preprocessor.Identifier(data, out token))
            {
                if (token.Id == Preprocessor.DefinedKeyword || token.Id == Preprocessor.VariadicKeyword)
                    return Preprocessor.ThrowError(context, PreprocessorCodes.ReservedKeyword);

                long streamPos = data.Position;
                data.Position += token.Length;

                while (!Preprocessor.LineBreak(data))
                {
                    Preprocessor.Whitespace(context, data, nodes);
                    if (!Preprocessor.Comment(context, data))
                        break;
                }

                if (Preprocessor.LineBreak(context, data, nodes))
                {
                    CreateToken(streamPos, line, column, token.Length, data, nodes, TokenTypes.IfNotDefined);
                    return true;
                }
            }
            return Preprocessor.ThrowError(context, PreprocessorCodes.IdentifierExpected);
        }
        private static bool If(CodeProcessorContext context, Stream data, TokenStream nodes, TokenTypes token = TokenTypes.If)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            CreateToken(context, data, nodes, token);
            return true;
        }
        private static bool Elif(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            nodes.Add(TokenTypes.TokenSeparator);

            if (If(context, data, nodes, TokenTypes.ElseIf))
                return true;

            return false;
        }
        private static bool Else(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            UInt32 line = (UInt32)context.LineNumber;
            UInt32 column = (UInt32)(data.Position - context.LinePosition);

            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            if (!Preprocessor.LineBreak(context, data, nodes))
                return Preprocessor.ThrowError(context, PreprocessorCodes.UnexpectedCharacter);

            nodes.Add(TokenTypes.TokenSeparator);
            nodes.Add(TokenTypes.Else);
            nodes.Encode(line);
            nodes.Encode(column);
            nodes.Add(TokenTypes.TokenSeparator);
            return true;
        }
        private static bool Endif(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(data))
            {
                Preprocessor.Whitespace(context, data, nodes);
                if (!Preprocessor.Comment(context, data))
                    break;
            }

            if (!Preprocessor.LineBreak(context, data, nodes))
                return Preprocessor.ThrowError(context, PreprocessorCodes.UnexpectedCharacter);

            nodes.Add(TokenTypes.TokenSeparator);
            return true;
        }
        public static bool Direktive(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (data.Peek() != '#')
                return false;

            long streamPos = data.Position;
            data.Get();

            Preprocessor.Whitespace(context, data, nodes);

            bool handled = false;
            long length = data.SkipLiteral();
            if (length > 0)
            {
                data.Position = streamPos + 1;
                string result = data.Read(length);
                switch (result)
                {
                    case "pragma": handled = Pragma(context, data, nodes); break;
                    case "include": handled = Include(context, data, nodes); break;
                    case "define": handled = Define(context, data, nodes); break;
                    case "undef": handled = Undefine(context, data, nodes); break;
                    case "error": handled = Error(context, data, nodes); break;
                    case "if": handled = If(context, data, nodes); break;
                    case "ifdef": handled = Ifdef(context, data, nodes); break;
                    case "ifndef": handled = Ifndef(context, data, nodes); break;
                    case "elif": handled = Elif(context, data, nodes); break;
                    case "else": handled = Else(context, data, nodes); break;
                    case "endif": handled = Endif(context, data, nodes); break;
                }
            }
            else
            {
                Preprocessor.Whitespace(context, data, nodes);
                handled = Preprocessor.LineBreak(context, data, nodes);
            }

            if (!handled) data.Position = streamPos;
            return handled;
        }

        private static void Process(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!Preprocessor.LineBreak(context, data, nodes))
                if (!Preprocessor.EscapedLineBreak(context, data, nodes) && !Preprocessor.Comment(context, data, nodes) && !Preprocessor.String(context, data, nodes))
                    nodes.Add(data.Get());
        }
        public static bool Any(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            nodes.Add(TokenTypes.Text);
            nodes.Encode((UInt32)context.LineNumber);
            nodes.Encode((UInt32)(data.Position - context.LinePosition));
            nodes.Add(TokenTypes.TokenSeparator);

            Process(context, data, nodes);

            nodes.Add('\n');
            nodes.Add(TokenTypes.TokenSeparator);
            return true;
        }
    }
}
