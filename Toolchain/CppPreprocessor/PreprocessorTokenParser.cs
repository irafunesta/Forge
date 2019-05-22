// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using SE.Forge.CppPreprocessor.Tokens;
using SE.Code.Parsing;
using SE.Code;
using SE.Text;

namespace SE.Forge.CppPreprocessor
{
    public static partial class Preprocessor
    {
        public static bool GetSyntaxTree(Stream data, SyntaxTree<PreprocessorToken> tree, PreprocessorToken root = null)
        {
            while (!data.Eof())
            {
                char c = data.Peek();
                switch ((TokenTypes)c)
                {
                    case TokenTypes.IfDefined:
                    case TokenTypes.IfNotDefined:
                    case TokenTypes.If:
                    case TokenTypes.ElseIf:
                        {
                            ConditionToken token = new ConditionToken((TokenTypes)c);
                            token.Deserialize(data);

                            if (root == null) tree.Add(token);
                            else tree.AddAppend(root, token);

                            if (!GetSyntaxTree(data, tree, token))
                                return false;
                        }
                        break;
                    case TokenTypes.Else:
                        {
                            PreprocessorToken token = new PreprocessorToken(TokenTypes.Else);
                            token.Deserialize(data);

                            if (root == null) tree.Add(token);
                            else tree.AddAppend(root, token);

                            if (!GetSyntaxTree(data, tree, token))
                                return false;
                        }
                        break;
                    case TokenTypes.Define:
                    case TokenTypes.Undefine:
                    case TokenTypes.Include:
                    case TokenTypes.Error:
                    case TokenTypes.Pragma:
                        {
                            TextToken token = new TextToken((TokenTypes)c);
                            token.Deserialize(data);

                            if (root == null) tree.Add(token);
                            else tree.AddAppend(root, token);
                        }
                        break;
                    case TokenTypes.Text:
                        {
                            TextToken token = new TextToken();
                            token.Deserialize(data);

                            if (root == null) tree.Add(token);
                            else tree.AddAppend(root, token);
                        }
                        break;
                    default:
                        {
                            if(root != null && c == 0)
                            {
                                data.Get();
                                return true;
                            }
                            else return ThrowError(new CodeProcessorContext(new CodeProcessorConfig()), PreprocessorCodes.UnexpectedCharacter);
                        }
                }
            }
            return true;
        }

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
        private static void Ifdef(CodeProcessorContext context, ConditionToken token, TokenStream nodes)
        {
            int bytes;
            CheckState(context, nodes, (byte)((context.Definitions.ContainsKey(token.Condition.DecodeUInt32(out bytes))) ? 0 : 1));

            if (nodes.CanWrite && token.Child != null)
                GetResult(context, token.Child as PreprocessorToken, nodes);

            EndCondition(context, token.Next, nodes);
        }
        private static void Ifndef(CodeProcessorContext context, ConditionToken token, TokenStream nodes)
        {
            int bytes;
            CheckState(context, nodes, (byte)((context.Definitions.ContainsKey(token.Condition.DecodeUInt32(out bytes))) ? 1 : 0));

            if (nodes.CanWrite && token.Child != null)
                GetResult(context, token.Child as PreprocessorToken, nodes);

            EndCondition(context, token.Next, nodes);
        }
        private static void If(CodeProcessorContext context, ConditionToken token, TokenStream nodes, bool useExistingState)
        {
            TokenStream expression = new TokenStream();
            Lazy<HashSet<UInt32>> stack = new Lazy<HashSet<UInt32>>();

            while (!LineBreak(context, token.Condition, expression))
                ProcessNoComments(context, token.Condition, expression, stack, true);

            int result; if (useExistingState) result = context.States.Pop();
            else result = 0;

            if (result >= 4)
                result = 2;

            result |= (PreprocessorEvaluator.Evaluate(expression) ? 0 : 1);
            CheckState(context, nodes, result);

            if (nodes.CanWrite && token.Child != null)
                GetResult(context, token.Child as PreprocessorToken, nodes);

            EndCondition(context, token.Next, nodes);
        }
        private static void Else(CodeProcessorContext context, PreprocessorToken token, TokenStream nodes)
        {
            int result = context.States.Pop();
            if (result >= 4)
                result = 2;

            CheckState(context, nodes, result);

            if (nodes.CanWrite && token.Child != null)
                GetResult(context, token.Child as PreprocessorToken, nodes);

            EndCondition(context, null, nodes);
        }

        private static void GetResult(CodeProcessorContext context, PreprocessorToken token, TokenStream nodes)
        {
            while (token != null)
            {
                switch ((TokenTypes)token.Type)
                {
                    case TokenTypes.IfDefined: Ifdef(context, token as ConditionToken, nodes); break;
                    case TokenTypes.IfNotDefined: Ifndef(context, token as ConditionToken, nodes); break;
                    case TokenTypes.If: If(context, token as ConditionToken, nodes, false); break;
                    case TokenTypes.ElseIf: If(context, token as ConditionToken, nodes, true); break;
                    case TokenTypes.Else: Else(context, token, nodes); break;
                    case TokenTypes.Define: 
                        if(!Define(context, (token as TextToken).Text, nodes))
                            ThrowError(context, PreprocessorCodes.InvalidToken);
                        break;
                    case TokenTypes.Undefine: 
                        if(!Undefine(context, (token as TextToken).Text, nodes))
                            ThrowError(context, PreprocessorCodes.InvalidToken);
                        break;
                    case TokenTypes.Include:
                        if (!Include(context, (token as TextToken).Text, nodes))
                            ThrowError(context, PreprocessorCodes.InvalidToken);
                        break;
                    case TokenTypes.Pragma: 
                        if(!Pragma(context, (token as TextToken).Text, nodes))
                            ThrowError(context, PreprocessorCodes.InvalidToken);
                        break;
                    case TokenTypes.Error: 
                        if(!Error(context, (token as TextToken).Text, nodes))
                            throw new Exception((token as TextToken).Text.ToString());
                        break;
                    case TokenTypes.Text: Any(context, (token as TextToken).Text, nodes); break;
                }
                token = token.Next as PreprocessorToken;
            }
        }
        public static void GetResult(SyntaxTree<PreprocessorToken> tree, TokenStream nodes)
        {
            GetResult(new CodeProcessorContext(new CodeProcessorConfig()), tree.Root, nodes);
        }
    }
}
