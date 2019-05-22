// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SE.Forge.CppPreprocessor.Tokens;
using SE.Forge.CppPreprocessor;
using SE.Code.Parsing;
using SE.Storage;
using SE.Code;
using SE.Text;
using SE.App;

namespace SE.Forge.CppAnalyzer
{
    public static class IncludeParser
    {
        private static void EndCondition(CodeProcessorContext context, Token token, TokenStream nodes)
        {
            byte type; if (token != null) type = token.Type;
            else type = 0;
            switch ((TokenTypes)type)
            {
                case TokenTypes.ElseIf:
                case TokenTypes.Else: break;
                default:
                    {
                        context.States.Pop();
                        bool enabled = true;
                        foreach (byte state in context.States)
                            if (state > 0 && state < 4)
                            {
                                enabled = false;
                                break;
                            }

                        nodes.Locked = !enabled;
                    }
                    break;
            }
        }
        private static void Ifdef(CodeProcessorContext context, ConditionToken token, List<FileDescriptor> referenceFiles, List<string> fileLookups, TokenStream nodes)
        {
            int bytes;
            Preprocessor.CheckState(context, nodes, (byte)((context.Definitions.ContainsKey(token.Condition.DecodeUInt32(out bytes))) ? 0 : 1));

            if (nodes.CanWrite && token.Child != null)
                GetResult(context, token.Child as PreprocessorToken, referenceFiles, fileLookups, nodes);

            EndCondition(context, token.Next, nodes);
        }
        private static void Ifndef(CodeProcessorContext context, ConditionToken token, List<FileDescriptor> referenceFiles, List<string> fileLookups, TokenStream nodes)
        {
            int bytes;
            Preprocessor.CheckState(context, nodes, (byte)((context.Definitions.ContainsKey(token.Condition.DecodeUInt32(out bytes))) ? 1 : 0));

            if (nodes.CanWrite && token.Child != null)
                GetResult(context, token.Child as PreprocessorToken, referenceFiles, fileLookups, nodes);

            EndCondition(context, token.Next, nodes);
        }
        private static void If(CodeProcessorContext context, ConditionToken token, List<FileDescriptor> referenceFiles, List<string> fileLookups, TokenStream nodes, bool useExistingState)
        {
            TokenStream expression = new TokenStream();
            Lazy<HashSet<UInt32>> stack = new Lazy<HashSet<UInt32>>();

            while (!Preprocessor.LineBreak(context, token.Condition, expression))
                Preprocessor.ProcessNoComments(context, token.Condition, expression, stack, true);

            int result; if (useExistingState) result = context.States.Pop();
            else result = 0;

            if (result >= 4)
                result = 2;

            result |= (PreprocessorEvaluator.Evaluate(expression) ? 0 : 1);
            Preprocessor.CheckState(context, nodes, result);

            if (nodes.CanWrite && token.Child != null)
                GetResult(context, token.Child as PreprocessorToken, referenceFiles, fileLookups, nodes);

            EndCondition(context, token.Next, nodes);
        }
        private static void Else(CodeProcessorContext context, List<FileDescriptor> referenceFiles, List<string> fileLookups, PreprocessorToken token, TokenStream nodes)
        {
            int result = context.States.Pop();
            if (result >= 4)
                result = 2;

            Preprocessor.CheckState(context, nodes, result);

            if (nodes.CanWrite && token.Child != null)
                GetResult(context, token.Child as PreprocessorToken, referenceFiles, fileLookups, nodes);

            EndCondition(context, null, nodes);
        }
        private static bool Include(CodeProcessorContext context, Stream data, List<FileDescriptor> referenceFiles, List<string> fileLookups, TokenStream nodes)
        {
            if (!Preprocessor.Whitespace(context, data, nodes))
                return Preprocessor.ThrowError(context, PreprocessorCodes.UnexpectedCharacter);

            TokenStream target = new TokenStream();
            Lazy<HashSet<UInt32>> stack = new Lazy<HashSet<UInt32>>();

            while (!Preprocessor.LineBreak(context, data, target))
                Preprocessor.Process(context, data, target, stack, true);

            if (Preprocessor.IncludeHeader(context, target, target))
            {
                fileLookups.Add(Encoding.UTF8.GetString(target.Buffer.ToArray()));
            }
            else if (Preprocessor.IncludeSource(context, target, target))
            {
                string path = Encoding.UTF8.GetString(target.Buffer.ToArray());
                FileDescriptor file = FileDescriptor.Create(context.File.Location, path);
                if (file.Exists())
                    referenceFiles.Add(file);
                else
                    fileLookups.Add(path);
            }
            else
                return Preprocessor.ThrowError(context, PreprocessorCodes.ExpressionExpected);
            return true;
        }

        public static void GetResult(CodeProcessorContext context, PreprocessorToken token, List<FileDescriptor> referenceFiles, List<string> fileLookups, TokenStream nodes)
        {
            while (token != null)
            {
                switch ((TokenTypes)token.Type)
                {
                    case TokenTypes.IfDefined: Ifdef(context, token as ConditionToken, referenceFiles, fileLookups, nodes); break;
                    case TokenTypes.IfNotDefined: Ifndef(context, token as ConditionToken, referenceFiles, fileLookups, nodes); break;
                    case TokenTypes.If: If(context, token as ConditionToken, referenceFiles, fileLookups, nodes, false); break;
                    case TokenTypes.ElseIf: If(context, token as ConditionToken, referenceFiles, fileLookups, nodes, true); break;
                    case TokenTypes.Else: Else(context, referenceFiles, fileLookups, token, nodes); break;
                    case TokenTypes.Define:
                        if (!Preprocessor.Define(context, (token as TextToken).Text, nodes))
                             Preprocessor.ThrowError(context, PreprocessorCodes.InvalidToken);
                        break;
                    case TokenTypes.Undefine:
                        if (!Preprocessor.Undefine(context, (token as TextToken).Text, nodes))
                            Preprocessor.ThrowError(context, PreprocessorCodes.InvalidToken);
                        break;
                    case TokenTypes.Include:
                        if (!Include(context, (token as TextToken).Text, referenceFiles, fileLookups, nodes))
                            Preprocessor.ThrowError(context, PreprocessorCodes.InvalidToken);
                        break;
                    case TokenTypes.Error:
                        if (ProjectLookup.IgnoreErrors)
                        {
                            Application.Warning(SeverityFlags.Full, "#error directive found: '{0}'", (token as TextToken).Text.ToString());
                            break;
                        }
                        else throw new Exception((token as TextToken).Text.ToString());
                    case TokenTypes.Text: Preprocessor.Any(context, (token as TextToken).Text, nodes); break;
                }
                token = token.Next as PreprocessorToken;
            }
        }
    }
}
