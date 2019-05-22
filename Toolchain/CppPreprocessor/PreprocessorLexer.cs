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
    public static partial class Preprocessor
    {
        private static void ReadLine(CodeProcessorContext context, Stream data, TokenStream nodes, long offset = 0)
        {
            while (!LineBreak(context, data, nodes))
            {
                if (SimpleWhitespace(data))
                {
                    if (nodes.Last != ' ' && (nodes.Length - offset) > 0 && !data.Eof())
                        nodes.Add(' ');
                }
                else if (!EscapedLineBreak(context, data) && !Comment(context, data) && !String(context, data, nodes))
                    nodes.Add(data.Get());
            }
        }

        private static bool FunctionMacro(CodeProcessorContext context, UInt32 id, Stream data, TokenStream nodes)
        {
            Macro macro = new Macro(id);
            data.Get();
            do
            {
                if (Whitespace(context, data, nodes))
                    continue;

                if (LineBreak(context, data, nodes))
                    return ThrowError(context, PreprocessorCodes.MissingBracket);

                long length = data.Skip(".");
                if (length > 0 && length != 3)
                    return ThrowError(context, PreprocessorCodes.VariadicParameterMismatch);

                IdentifierToken parameter;
                if (length == 3)
                {
                    macro.Variadic = true;
                    break;
                }
                else if (Identifier(data, out parameter))
                {
                    if (parameter.Id == VariadicKeyword)
                        return ThrowError(context, PreprocessorCodes.VariadicMacroInParameterList);

                    data.Position += parameter.Length;
                    macro.Parameter.Add(parameter.Id);
                }
                else if (data.Peek() == ',')
                    data.Get();
                else
                    return ThrowError(context, PreprocessorCodes.UnexpectedCharacter);
            }
            while (data.Peek() != ')');
            if (data.Peek() != ')')
                return ThrowError(context, PreprocessorCodes.MissingBracket);

            data.Get();

            Whitespace(context, data, nodes);

            ReadLine(context, data, macro.ReplacementList);

            if (nodes.CanWrite)
            {
                if (context.Definitions.ContainsKey(id))
                {
                    return ThrowError(context, PreprocessorCodes.AlreadyDefined);
                }
                else context.Definitions.Add(id, macro);
            }
            return true;
        }
        public static bool Define(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            IdentifierToken token; if (Identifier(data, out token))
            {
                if (token.Id == DefinedKeyword || token.Id == VariadicKeyword)
                    return ThrowError(context, PreprocessorCodes.ReservedKeyword);

                data.Position += token.Length;
                if (data.Peek() == '(') return FunctionMacro(context, token.Id, data, nodes);
                else
                {
                    Whitespace(context, data, nodes);

                    Macro macro = new Macro(token.Id);
                    ReadLine(context, data, macro.ReplacementList);
                    if (nodes.CanWrite)
                    {
                        if (context.Definitions.ContainsKey(token.Id))
                        {
                            return ThrowError(context, PreprocessorCodes.AlreadyDefined);
                        }
                        else context.Definitions.Add(token.Id, macro);
                    }
                    return true;
                }
            }
            return ThrowError(context, PreprocessorCodes.IdentifierExpected);
        }
        public static bool Undefine(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            IdentifierToken token; if (Identifier(data, out token))
            {
                if (token.Id == DefinedKeyword || token.Id == VariadicKeyword)
                    return ThrowError(context, PreprocessorCodes.ReservedKeyword);

                data.Position += token.Length;
                while (!LineBreak(data))
                {
                    Whitespace(context, data, nodes);
                    if (!Comment(context, data))
                        break;
                }

                if (LineBreak(context, data, nodes))
                {
                    if (!context.Definitions.ContainsKey(token.Id))
                    {
                        return ThrowError(context, PreprocessorCodes.NotDefined);
                    }
                    else context.Definitions.Remove(token.Id);
                    return true;
                }
            }
            return ThrowError(context, PreprocessorCodes.IdentifierExpected);
        }

        public static bool IncludeHeader(CodeProcessorContext context, TokenStream data, TokenStream nodes)
        {
            if (data.Peek() != '<')
                return false;

            long streamPos = data.Position;
            data.Get();

            while (!data.Eof() && data.Peek() != '>')
            {
                if(!Whitespace(context, data, nodes))
                    data.Get();
            }

            if (data.Peek() == '>')
            {
                data.Get();

                Whitespace(context, data, nodes);
                if (LineBreak(context, data, nodes))
                {
                    while (" \r\n\t".IndexOf(data.Last) >= 0)
                        data.Remove();

                    data.Buffer.RemoveAt(0);
                    data.Remove();
                    return true;
                }
            }
            return ThrowError(context, PreprocessorCodes.UnexpectedCharacter);
        }
        public static bool IncludeSource(CodeProcessorContext context, TokenStream data, TokenStream nodes)
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
                        Whitespace(context, data, nodes);
                        if (LineBreak(context, data, nodes))
                        {
                            while (" \r\n\t".IndexOf(data.Last) >= 0)
                                data.Remove();

                            data.Buffer.RemoveAt(0);
                            data.Remove();
                            return true;
                        }
                        else ThrowError(context, PreprocessorCodes.UnexpectedCharacter);
                    }
                    else if (LineBreak(context, data, nodes))
                        break;
                }
            }

            data.Position = streamPos;
            return false;
        }
        public static bool Include(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            TokenStream target = new TokenStream();
            Lazy<HashSet<UInt32>> stack = new Lazy<HashSet<UInt32>>();

            while (!LineBreak(context, data, target))
                ProcessNoComments(context, data, target, stack, true);

            if (IncludeHeader(context, target, target))
                context.States.Push(PreprocessorCodes.IncludeHeader);
            else if (IncludeSource(context, target, target))
                context.States.Push(PreprocessorCodes.IncludeSource);
            else
                return ThrowError(context, PreprocessorCodes.ExpressionExpected);

            if (context.Callback != null)
                return context.Callback(context, target, nodes);

            return true;
        }

        public static bool Error(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            TokenStream message = new TokenStream();
            ReadLine(context, data, message);

            if (!ThrowError(context, data))
                throw new Exception(message.ToString());

            return true;
        }
        public static bool Pragma(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            TokenStream config = new TokenStream();
            ReadLine(context, data, config);

            if (context.Callback != null)
            {
                context.States.Push(PreprocessorCodes.Config);
                return context.Callback(context, config, nodes);
            }
            return true;
        }

        private static bool Defined(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            data.Position += 7;
            Whitespace(context, data, nodes);

            IdentifierToken token;
            if (data.Peek() == '(')
            {
                data.Get();

                Whitespace(context, data, nodes);
                if (!Identifier(data, out token))
                    return ThrowError(context, PreprocessorCodes.IdentifierExpected);

                data.Position += token.Length;
                Whitespace(context, data, nodes);

                if (data.Peek() != ')')
                    return ThrowError(context, PreprocessorCodes.MissingBracket);

                data.Get();
            }
            else if (!Identifier(data, out token))
                return ThrowError(context, PreprocessorCodes.IdentifierExpected);
            else
                data.Position += token.Length;
            if (context.Definitions.ContainsKey(token.Id))
                nodes.Add('1');
            else
                nodes.Add('0');
            return true;
        }

        public static void CheckState(CodeProcessorContext context, TokenStream nodes, int result)
        {
            if (result == 0)
            {
                bool enabled = true;
                foreach (byte state in context.States)
                    if (state > 0 && state < 4)
                    {
                        enabled = false;
                        break;
                    }

                nodes.Locked = !enabled;
                context.States.Push(1 << 2);
            }
            else
            {
                context.States.Push((byte)(result & (1 << 1)));
                nodes.Locked = true;
            }
        }

        private static bool Ifdef(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            IdentifierToken token; if (Identifier(data, out token))
            {
                if (token.Id == DefinedKeyword || token.Id == VariadicKeyword)
                    return ThrowError(context, PreprocessorCodes.ReservedKeyword);

                data.Position += token.Length;
                while (!LineBreak(data))
                {
                    Whitespace(context, data, nodes);
                    if (!Comment(context, data))
                        break;
                }

                if (LineBreak(context, data, nodes))
                {
                    CheckState(context, nodes, (context.Definitions.ContainsKey(token.Id)) ? 0 : 1);
                    return true;
                }
            }
            return ThrowError(context, PreprocessorCodes.IdentifierExpected);
        }
        private static bool Ifndef(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            IdentifierToken token; if (Identifier(data, out token))
            {
                if (token.Id == DefinedKeyword || token.Id == VariadicKeyword)
                    return ThrowError(context, PreprocessorCodes.ReservedKeyword);

                data.Position += token.Length;
                while (!LineBreak(data))
                {
                    Whitespace(context, data, nodes);
                    if (!Comment(context, data))
                        break;
                }

                if (LineBreak(context, data, nodes))
                {
                    CheckState(context, nodes, (context.Definitions.ContainsKey(token.Id)) ? 1 : 0);
                    return true;
                }
            }
            return ThrowError(context, PreprocessorCodes.IdentifierExpected);
        }
        private static bool If(CodeProcessorContext context, Stream data, TokenStream nodes, bool useExistingState = false)
        {
            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            TokenStream expression = new TokenStream();
            Lazy<HashSet<UInt32>> stack = new Lazy<HashSet<UInt32>>();

            while (!LineBreak(context, data, expression))
                ProcessNoComments(context, data, expression, stack, true);

            int result; if (useExistingState) result = context.States.Pop();
            else result = 0;

            if (result >= 4)
                result = 2;

            result |= (PreprocessorEvaluator.Evaluate(expression) ? 0 : 1);

            CheckState(context, nodes, result);
            return true;
        }
        private static bool Elif(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (context.States.Count == 0)
                return ThrowError(context, PreprocessorCodes.ElseWithoutIf);

            if (If(context, data, nodes, true))
                return true;

            return false;
        }
        private static bool Else(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (context.States.Count == 0)
                return ThrowError(context, PreprocessorCodes.ElseWithoutIf);

            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            if (!LineBreak(context, data, nodes))
                return ThrowError(context, PreprocessorCodes.UnexpectedCharacter);

            int result = context.States.Pop();
            if (result >= 4)
                result = 2;

            CheckState(context, nodes, result);
            return true;
        }
        private static bool Endif(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (context.States.Count == 0)
                return ThrowError(context, PreprocessorCodes.EndWithoutIf);

            while (!LineBreak(data))
            {
                Whitespace(context, data, nodes);
                if (!Comment(context, data))
                    break;
            }

            if (!LineBreak(context, data, nodes))
                return ThrowError(context, PreprocessorCodes.UnexpectedCharacter);

            context.States.Pop();
            bool enabled = true;
            foreach (byte state in context.States)
                if (state > 0 && state < 4)
                {
                    enabled = false;
                    break;
                }

            nodes.Locked = !enabled;
            return true;
        }

        public static bool Direktive(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            if (data.Peek() != '#')
                return false;

            long streamPos = data.Position;
            data.Get();

            Whitespace(context, data, nodes);

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
                Whitespace(context, data, nodes);
                handled = LineBreak(context, data, nodes);
            }

            if (!handled) data.Position = streamPos;
            return handled;
        }

        private static bool Stringifier(CodeProcessorContext context, Stream data, TokenStream nodes, out IdentifierToken token)
        {
            if (data.Peek() != '#')
            {
                token = new IdentifierToken();
                return false;
            }

            long streamPos = data.Position;
            data.Get();

            Whitespace(context, data, nodes);

            if (Identifier(data, out token))
                return true;

            data.Position = streamPos;
            return false;
        }
        private static bool Stringify(CodeProcessorContext context, Macro macro, TokenStream nodes, List<TokenStream> parameter)
        {
            IdentifierToken token; if (Stringifier(context, macro.ReplacementList, nodes, out token))
            {
                if (token.Id == VariadicKeyword)
                {
                    nodes.Add('"');
                    for (int i = macro.Parameter.Count; i < parameter.Count; i++)
                    {
                        parameter[i].Position = 0;
                        ReadLine(context, parameter[i], nodes);
                        if (i + 1 < parameter.Count)
                            nodes.Add(',');
                    }
                    nodes.Add('"');
                    macro.ReplacementList.Position += token.Length;
                }
                else if ((token.Id = (UInt32)macro.Parameter.IndexOf(token.Id)) != UInt32.MaxValue)
                {
                    nodes.Add('"');
                    parameter[(int)token.Id].Position = 0;
                    ReadLine(context, parameter[(int)token.Id], nodes, nodes.Length);
                    nodes.Add('"');
                    macro.ReplacementList.Position += token.Length;
                }
                else return ThrowError(context, PreprocessorCodes.ParameterExpected);
                return true;
            }
            return false;
        }

        private static bool TokenPasting(CodeProcessorContext context, Stream data, TokenStream nodes, bool hasVariableParameter)
        {
            char c = data.Peek();
            if (c != '#' && c != ' ' && c != '\t' && c != '\r')
                return false;

            long streamPos = data.Position;
            Whitespace(context, data, nodes);

            long length = data.Skip("#");
            if (length == 2)
            {
                if (streamPos == 0)
                    return ThrowError(context, PreprocessorCodes.StartOfFile);

                Whitespace(context, data, nodes);

                if (data.Eof())
                    return ThrowError(context, PreprocessorCodes.EndOfFile);

                return true;
            }

            data.Position = streamPos;
            return false;
        }
        private static bool TokenPasting(CodeProcessorContext context, Macro macro, TokenStream nodes, List<TokenStream> parameter, bool hasVariableParameter)
        {
            if (TokenPasting(context, macro.ReplacementList, nodes, hasVariableParameter))
            {
                IdentifierToken token; if ((Identifier(macro.ReplacementList, out token) && token.Id == VariadicKeyword))
                {
                    long position = nodes.Length;
                    if (ParameterReplacement(context, macro, nodes, parameter) && !hasVariableParameter && nodes.Last == ',')
                        nodes.Remove();
                    else if (position < nodes.Length)
                    {
                        while (nodes.Buffer[(int)position] == ' ')
                            nodes.Buffer.RemoveAt((int)position);
                    }
                }
                return true;
            }
            else return false;
        }
        private static bool ParameterReplacement(CodeProcessorContext context, Macro macro, TokenStream nodes, List<TokenStream> parameter)
        {
            IdentifierToken token; if (Identifier(macro.ReplacementList, out token))
            {
                if (token.Id == VariadicKeyword)
                {
                    for (int i = macro.Parameter.Count; i < parameter.Count; i++)
                    {
                        parameter[i].Position = 0;
                        ReadLine(context, parameter[i], nodes);
                        if (i + 1 < parameter.Count)
                            nodes.Add(',');
                    }
                    macro.ReplacementList.Position += token.Length;
                }
                else if ((token.Id = (UInt32)macro.Parameter.IndexOf(token.Id)) != UInt32.MaxValue)
                {
                    parameter[(int)token.Id].Position = 0;
                    ReadLine(context, parameter[(int)token.Id], nodes, nodes.Length);
                    macro.ReplacementList.Position += token.Length;
                }
                else
                    macro.ReplacementList.CopyRange(nodes, (int)token.Length);

                return true;
            }
            return false;
        }

        private static bool FunctionMacroParameter(CodeProcessorContext context, Stream data, List<TokenStream> parameter)
        {
            TokenStream stream = new TokenStream();
            data.Get();
            do
            {
                data.Skip("\r");
                LineBreak(context, data, stream);

                if (data.Peek() == '(')
                {
                    data.Get();

                    long length = data.FindRelated("(", ")");
                    if (data.Peek() != ')')
                        break;

                    length++;
                    data.Position -= length;
                    length++;

                    data.CopyRange(stream, (int)length);
                }
                if (data.Peek() == ',')
                {
                    parameter.Add(stream);
                    stream = new TokenStream();
                    data.Get();
                }
                if (data.Peek() != ')')
                    stream.Add(data.Get());
            }
            while (!data.Eof() && data.Peek() != ')');
            if (data.Peek() == ')')
            {
                parameter.Add(stream);
                stream = null;
                data.Get();

                return true;
            }
            return ThrowError(context, PreprocessorCodes.ParameterExpected);
        }
        private static bool ExpandFunctionMacro(CodeProcessorContext context, Stream data, TokenStream nodes, Lazy<HashSet<UInt32>> stack, Macro macro)
        {
            if (stack.Value.Contains(macro.Id))
                return false;

            Whitespace(context, data, nodes);

            if (data.Peek() == '(')
            {
                long streamPos = data.Position;

                List<TokenStream> parameter = new List<TokenStream>();
                if (FunctionMacroParameter(context, data, parameter))
                {
                    if ((macro.Parameter.Count != parameter.Count && !macro.Variadic))
                        return ThrowError(context, PreprocessorCodes.ParameterMismatch);

                    bool hasVariableParameter = ((parameter.Count - macro.Parameter.Count) > 0);
                    macro.ReplacementList.Position = 0;

                    TokenStream preparsed = new TokenStream();
                    while (!macro.ReplacementList.Eof())
                    {
                        if
                        (
                            !Stringify(context, macro, preparsed, parameter) &&
                            !TokenPasting(context, macro, preparsed, parameter, hasVariableParameter) &&
                            !ParameterReplacement(context, macro, preparsed, parameter) &&
                            !String(context, macro.ReplacementList, preparsed) &&
                            !EscapedLineBreak(context, macro.ReplacementList) &&
                            !Comment(context, macro.ReplacementList)
                        )
                            preparsed.Add(macro.ReplacementList.Get());
                    }
                    TokenStream result = new TokenStream();
                    while (!preparsed.Eof())
                    {
                        IdentifierToken token; if (Identifier(preparsed, out token))
                        {
                            object objectMacro; if (context.Definitions.TryGetValue(token.Id, out objectMacro) && (objectMacro as Macro).Parameter.Count == 0 && !(objectMacro as Macro).Variadic)
                            {
                                long tmp = preparsed.Position;
                                preparsed.Position += token.Length;

                                if (ExpandObjectMacro(context, preparsed, result, stack, (objectMacro as Macro)))
                                    continue;

                                preparsed.Position = tmp;
                            }
                            preparsed.CopyRange(result, (int)token.Length);
                        }
                        else if (!String(context, preparsed, result) && !Comment(context, macro.ReplacementList))
                            result.Add(preparsed.Get());
                    }

                    AnyPreventLineBreak(context, data, result, stack);

                    stack.Value.Add(macro.Id);

                    AnyPreventLineBreak(context, result, nodes, stack);

                    stack.Value.Remove(macro.Id);
                    return true;
                }

                data.Position = streamPos;
            }
            return false;
        }
        private static bool ExpandObjectMacro(CodeProcessorContext context, Stream data, TokenStream nodes, Lazy<HashSet<UInt32>> stack, Macro macro)
        {
            if (stack.Value.Contains(macro.Id))
                return false;

            macro.ReplacementList.Position = 0;

            TokenStream result = new TokenStream();
            while (!macro.ReplacementList.Eof())
            {
                if (!TokenPasting(context, macro.ReplacementList, result, false))
                    result.Add(macro.ReplacementList.Get());
            }

            AnyPreventLineBreak(context, data, result, stack);

            stack.Value.Add(macro.Id);

            AnyPreventLineBreak(context, result, nodes, stack);

            stack.Value.Remove(macro.Id);
            return true;
        }
        private static bool ExpandMacro(CodeProcessorContext context, Stream data, TokenStream nodes, Lazy<HashSet<UInt32>> stack, Macro macro)
        {
            if (macro.Parameter.Count > 0 || macro.Variadic) return ExpandFunctionMacro(context, data, nodes, stack, macro);
            else return ExpandObjectMacro(context, data, nodes, stack, macro);
        }

        internal static void Process(CodeProcessorContext context, Stream data, TokenStream nodes, Lazy<HashSet<UInt32>> stack, bool definedConditions = false)
        {
            IdentifierToken token; if (Identifier(data, out token))
            {
                if (definedConditions && token.Id == DefinedKeyword)
                {
                    Defined(context, data, nodes);
                    return;
                }

                object macro; if (context.Definitions.TryGetValue(token.Id, out macro))
                {
                    long streamPos = data.Position;
                    data.Position += token.Length;

                    if (ExpandMacro(context, data, nodes, stack, (macro as Macro)))
                        return;

                    data.Position = streamPos;
                }
                data.CopyRange(nodes, (int)token.Length);
            }
            else if (!EscapedLineBreak(context, data, nodes) && !Comment(context, data, nodes) && !String(context, data, nodes))
                nodes.Add(data.Get());
        }
        internal static void ProcessNoComments(CodeProcessorContext context, Stream data, TokenStream nodes, Lazy<HashSet<UInt32>> stack, bool definedConditions = false)
        {
            IdentifierToken token; if (Identifier(data, out token))
            {
                if (definedConditions && token.Id == DefinedKeyword)
                {
                    Defined(context, data, nodes);
                    return;
                }

                object macro; if (context.Definitions.TryGetValue(token.Id, out macro))
                {
                    long streamPos = data.Position;
                    data.Position += token.Length;

                    if (ExpandMacro(context, data, nodes, stack, (macro as Macro)))
                        return;

                    data.Position = streamPos;
                }
                data.CopyRange(nodes, (int)token.Length);
            }
            else if (!EscapedLineBreak(context, data) && !Comment(context, data) && !String(context, data, nodes))
                nodes.Add(data.Get());
        }
        
        private static bool AnyPreventLineBreak(CodeProcessorContext context, Stream data, TokenStream nodes, Lazy<HashSet<UInt32>> stack)
        {
            while (!LineBreak(data))
                ProcessNoComments(context, data, nodes, stack);

            return true;
        }
        private static bool AnyProcessLineBreak(CodeProcessorContext context, Stream data, TokenStream nodes, Lazy<HashSet<UInt32>> stack)
        {
            while (!LineBreak(context, data, nodes))
                Process(context, data, nodes, stack);

            return true;
        }
        public static bool Any(CodeProcessorContext context, Stream data, TokenStream nodes)
        {
            bool result = false;
            if (nodes.CanWrite)
            {
                result = AnyProcessLineBreak(context, data, nodes, new Lazy<HashSet<UInt32>>());
                if (result)
                    nodes.Add('\n');
            }
            else
            {
                while (!Preprocessor.LineBreak(context, data, nodes))
                    if (!Preprocessor.EscapedLineBreak(context, data, nodes) && !Preprocessor.Comment(context, data, nodes) && !Preprocessor.String(context, data, nodes))
                        data.Position++;

                result = true;
            }
            return result;
        }
    }
}
