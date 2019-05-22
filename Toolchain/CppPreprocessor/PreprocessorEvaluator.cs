// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using SE.Text;
using SE.App;
using SE.Code.Parsing;

namespace SE.Forge.CppPreprocessor
{
    public static class PreprocessorEvaluator
    {
        enum OperatorType
        {
            None,

            Add,
            Sub,
            Mult,
            Div,
            Mod,
            Increment,
            Decrement,

            BitAnd,
            BitExclusiveOr,
            BitInclusiveOr,
            LBitShift,
            RBitShift,

            Not,
            Equal,
            NEqual,
            LogicalAnd,
            LogicalExclusiveOr,
            LogicalInclusiveOr,
            LeftRelation,
            LeftRelationEqual,
            RightRelation,
            RightRelationEqual,

            SelectOperator,
            ReverseOperator,
            AlternateOperator,
        }

        private static bool Whitespace(Stream data)
        {
            return (data.Skip(" \t\r\n") > 0);
        }
        private static bool Boolean(Stream data, out object boolean)
        {
            boolean = null;

            char c = data.Peek();
            if (c != 't' && c != 'f') return false;

            long streamPos = data.Position;
            long length = data.SkipLiteral();
            if (length == 0 || length > 5)
            {
                data.Position = streamPos;
                return false;
            }

            data.Position = streamPos;
            string streamBuff = data.Read(length);
            if (c == 't' && length == 4 && streamBuff == "true")
            {
                boolean = (long)1;
                return true;
            }
            else if (c == 'f' && length == 5 && streamBuff == "false")
            {
                boolean = (long)0;
                return true;
            }
            data.Position = streamPos;
            return false;
        }

        private static bool UnsignedSuffix(Stream data)
        {
            char c = data.Peek();
            if (c == 'u' || c == 'U')
            {
                data.Get();
                return true;
            }
            return false;
        }
        private static bool Hex(Stream data)
        {
            if (data.Peek() != '0') return false;
            long streamPos = data.Position;

            data.Get(); if (data.Peek() != 'x')
            {
                data.Position = streamPos;
                return false;
            }

            data.Get();
            if (data.SkipNumeral("aAbBcCdDeEfF") == 0)
            {
                data.Position = streamPos;
                return false;
            }

            streamPos += 2;
            data.Position = streamPos;
            return true;
        }
        private static bool Integer(Stream data, out object number)
        {
            number = null;

            long streamPos = data.Position;
            bool isHex = false;
            if (data.Peek() == '0')
                isHex = Hex(data);

            if (data.SkipNumeral() == 0 || data.Peek() == '.' || data.Peek() == 'E' || data.Peek() == 'e')
            {
                data.Position = streamPos;
                return false;
            }

            bool isUnsigned = UnsignedSuffix(data);
            long length = data.Position - streamPos;
            data.Position = streamPos;

            if (isHex)
            {
                data.Position += 2;
                length -= 2;
            }

            if (isUnsigned) number = UInt64.Parse(data.Read(length), (isHex) ? NumberStyles.HexNumber : NumberStyles.Any, CultureInfo.InvariantCulture);
            else number = Int64.Parse(data.Read(length), (isHex) ? NumberStyles.HexNumber : NumberStyles.Any, CultureInfo.InvariantCulture);
            return true;
        }

        private static bool Identifier(Stream data)
        {
            char c = data.Peek();
            if (c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                long streamPos = data.Position;
                long length = data.SkipLiteral("_", true);
                if (length > 0)
                    return true;
            }

            return false;
        }

        private static OperatorType Operator(Stream data)
        {
            OperatorType result = OperatorType.None;

            char c = data.Peek();
            if (c != '=' && c != '*' && c != '/' && c != '+' && c != '-' && c != '!' && c != '?' && c != '<' && c != '>' &&
               c != '|' && c != ':' && c != '~' && c != '%' && c != '&' && c != '^') return result;

            long streamPos = data.Position; data.Get();
            long last = data.Skip("=+-*/&|^<>");

            if (last > 2) last = 2;
            last++;

            data.Position = streamPos;
            string streamBuff = data.Read(last);
            switch (c)
            {
                case ':':
                    {
                        result = OperatorType.AlternateOperator;
                        break;
                    }
                case '!':
                    // !=
                    if (last > 1 && streamBuff == "!=")
                    {
                        result = OperatorType.NEqual;
                        break;
                    }
                    // !
                    else
                    {
                        result = OperatorType.Not;
                        break;
                    }
                case '=':
                    if (last > 1 && streamBuff == "==")
                    {
                        result = OperatorType.Equal;
                    }
                    break;
                case '*':
                    {
                        result = OperatorType.Mult;
                        break;
                    }
                case '/':
                    {
                        result = OperatorType.Div;
                        break;
                    }
                case '+':
                    // ++
                    if (last > 1 && streamBuff == "++")
                    {
                        result = OperatorType.Increment;
                        break;
                    }
                    // +
                    else
                    {
                        result = OperatorType.Add;
                        break;
                    }
                case '-':
                    // --
                    if (last > 1 && streamBuff == "--")
                    {
                        result = OperatorType.Decrement;
                        break;
                    }
                    // -
                    else
                    {
                        result = OperatorType.Sub;
                        break;
                    }
                case '%':
                    {
                        result = OperatorType.Mod;
                        break;
                    }
                case '&':
                    // &&
                    if (last > 1 && streamBuff == "&&")
                    {
                        result = OperatorType.LogicalAnd;
                        break;
                    }
                    // &
                    else
                    {
                        result = OperatorType.BitAnd;
                        break;
                    }
                case '|':
                    // ||
                    if (last > 1 && streamBuff == "||")
                    {
                        result = OperatorType.LogicalInclusiveOr;
                        break;
                    }
                    // |
                    else
                    {
                        result = OperatorType.BitInclusiveOr;
                        break;
                    }
                case '^':
                    // ^^
                    if (last > 1 && streamBuff == "^^")
                    {
                        result = OperatorType.LogicalExclusiveOr;
                        break;
                    }
                    // ^
                    else
                    {
                        result = OperatorType.BitExclusiveOr;
                        break;
                    }
                case '<':
                    // <<
                    if (last > 1 && streamBuff == "<<")
                    {
                        result = OperatorType.LBitShift;
                        break;
                    }
                    // <=
                    else if (last > 1 && streamBuff == "<=")
                    {
                        result = OperatorType.LeftRelationEqual;
                        break;
                    }
                    // <
                    else
                    {
                        result = OperatorType.LeftRelation;
                        break;
                    }
                case '>':
                    // >>
                    if (last > 1 && streamBuff == ">>")
                    {
                        result = OperatorType.RBitShift;
                        break;
                    }
                    // >=
                    else if (last > 1 && streamBuff == ">=")
                    {
                        result = OperatorType.RightRelationEqual;
                        break;
                    }
                    // >
                    else
                    {
                        result = OperatorType.RightRelation;
                        break;
                    }
                case '~':
                    {
                        // ~
                        result = OperatorType.ReverseOperator;
                        break;
                    }
                case '?':
                    {
                        // ?
                        result = OperatorType.SelectOperator;
                        break;
                    }
            }

            data.Position = streamPos;
            return result;
        }

        private static object Primary(Stream data)
        {
            if (Identifier(data)) return (long)0;
            else if (data.Peek() == '(')
            {
                data.Get();

                Whitespace(data);

                dynamic result = Expression(data);

                Whitespace(data);

                if (data.Get() != ')')
                    return Preprocessor.ThrowError(new CodeProcessorContext(new CodeProcessorConfig()), PreprocessorCodes.MissingBracket);

                return result;
            }
            else
            {
                object result = 0; if (Integer(data, out result))
                    return result;
                else if (Boolean(data, out result))
                    return result;

                return Preprocessor.ThrowError(new CodeProcessorContext(new CodeProcessorConfig()), PreprocessorCodes.UnexpectedCharacter);
            }
        }
        private static object Unary(Stream data)
        {
            Whitespace(data);

            OperatorType op = Operator(data);

            Whitespace(data);

            switch (op)
            {
                case OperatorType.Add:
                    {
                        data.Position++;
                        return +(dynamic)Unary(data);
                    }
                case OperatorType.Sub:
                    {
                        data.Position++;
                        return -(dynamic)Unary(data);
                    }
                
                case OperatorType.ReverseOperator:
                    {
                        data.Position++;
                        return ~(dynamic)Unary(data);
                    }
                case OperatorType.Not:
                    {
                        data.Position++;
                        return (((dynamic)Unary(data) == 1) ? 0 : 1);
                    }
                default:
                    return Primary(data);
            }
        }

        private static object Multiplicative(Stream data)
        {
            dynamic result = Unary(data);

            OperatorType op;
            do
            {
                Whitespace(data);

                op = Operator(data);

                Whitespace(data);

                switch (op)
                {
                    case OperatorType.Div:
                        {
                            data.Position++;
                            result /= (dynamic)Unary(data);
                            break;
                        }
                    case OperatorType.Mod:
                        {
                            data.Position++;
                            result %= (dynamic)Unary(data);
                            break;
                        }
                    case OperatorType.Mult:
                        {
                            data.Position++;
                            result *= (dynamic)Unary(data);
                            break;
                        }
                }
            }
            while (op == OperatorType.Div || op == OperatorType.Mod || op == OperatorType.Mult);
            return result;
        }
        private static object Additive(Stream data)
        {
            dynamic result = Multiplicative(data);
            
            OperatorType op;
            do
            {
                Whitespace(data);

                op = Operator(data);

                Whitespace(data);

                switch (op)
                {
                    case OperatorType.Add:
                        {
                            data.Position ++;
                            result += (dynamic)Multiplicative(data);
                            break;
                        }
                    case OperatorType.Sub:
                        {
                            data.Position ++;
                            result -= (dynamic)Multiplicative(data);
                            break;
                        }
                }
            }
            while (op == OperatorType.Add || op == OperatorType.Sub);
            return result;
        }

        private static object Shift(Stream data)
        {
            dynamic result = Additive(data);

            OperatorType op;
            do
            {
                Whitespace(data);

                op = Operator(data);

                Whitespace(data);

                switch (op)
                {
                    case OperatorType.LBitShift:
                        {
                            data.Position += 2;
                            result <<= (dynamic)Additive(data);
                            break;
                        }
                    case OperatorType.RBitShift:
                        {
                            data.Position += 2;
                            result >>= (dynamic)Additive(data);
                            break;
                        }
                }
            }
            while (op == OperatorType.LBitShift || op == OperatorType.RBitShift);
            return result;
        }

        private static object Equality(Stream data)
        {
            dynamic result = Shift(data);

            OperatorType op;
            do
            {
                Whitespace(data);

                op = Operator(data);

                Whitespace(data);

                switch (op)
                {
                    case OperatorType.Equal:
                        {
                            data.Position += 2;
                            result = (result == (dynamic)Shift(data)) ? 1 : 0;
                            break;
                        }
                    case OperatorType.NEqual:
                        {
                            data.Position += 2;
                            result = (result != (dynamic)Shift(data)) ? 1 : 0;
                            break;
                        }
                    case OperatorType.LeftRelation:
                        {
                            data.Position++;
                            result = (result < (dynamic)Shift(data)) ? 1 : 0;
                            break;
                        }
                    case OperatorType.RightRelation:
                        {
                            data.Position ++;
                            result = (result > (dynamic)Shift(data)) ? 1 : 0;
                            break;
                        }
                    case OperatorType.LeftRelationEqual:
                        {
                            data.Position += 2;
                            result = (result <= (dynamic)Shift(data)) ? 1 : 0;
                            break;
                        }
                    case OperatorType.RightRelationEqual:
                        {
                            data.Position += 2;
                            result = (result >= (dynamic)Shift(data)) ? 1 : 0;
                            break;
                        }
                }
            }
            while (op == OperatorType.Equal || op == OperatorType.NEqual || 
                   op == OperatorType.LeftRelation || op == OperatorType.LeftRelationEqual || 
                   op == OperatorType.RightRelation || op == OperatorType.RightRelationEqual);

            return result;
        }
        private static object BitwiseAnd(Stream data)
        {
            dynamic result = Equality(data);

            Whitespace(data);

            while (Operator(data) == OperatorType.BitAnd)
            {
                data.Position++;
                dynamic left = result;

                Whitespace(data);

                dynamic right = Equality(data);
                result = (left & right);

                Whitespace(data);
            }
            return result;
        }
        private static object BitwiseXor(Stream data)
        {
            dynamic result = BitwiseAnd(data);

            Whitespace(data);

            while (Operator(data) == OperatorType.BitExclusiveOr)
            {
                data.Position ++;
                dynamic left = result;

                Whitespace(data);

                dynamic right = BitwiseAnd(data);
                result = (left ^ right);

                Whitespace(data);
            }
            return result;
        }
        private static object BitwiseOr(Stream data)
        {
            dynamic result = BitwiseXor(data);

            Whitespace(data);

            while (Operator(data) == OperatorType.BitInclusiveOr)
            {
                data.Position ++;
                dynamic left = result;

                Whitespace(data);

                dynamic right = BitwiseXor(data);
                result = (left | right);

                Whitespace(data);
            }
            return result;
        }
        private static object LogicalAnd(Stream data)
        {
            dynamic result = BitwiseOr(data);

            Whitespace(data);

            while (Operator(data) == OperatorType.LogicalAnd)
            {
                data.Position += 2;
                dynamic left = result;

                Whitespace(data);

                dynamic right = BitwiseOr(data);
                result = (((left != 0) && (right != 0)) ? 1 : 0);

                Whitespace(data);
            }
            return result;
        }
        private static object LogicalOr(Stream data)
        {
            dynamic result = LogicalAnd(data);

            Whitespace(data);

            while (Operator(data) == OperatorType.LogicalInclusiveOr)
            {
                data.Position += 2;
                dynamic left = result;

                Whitespace(data);

                dynamic right = LogicalAnd(data);
                result = (((left != 0) || (right != 0)) ? 1 : 0);

                Whitespace(data);
            }
            return result;
        }
        
        public static object Expression(Stream data)
        {
            dynamic result = LogicalOr(data);
            if (data.Peek() == '?')
            {
                data.Get();

                Whitespace(data);

                dynamic left = Expression(data);

                Whitespace(data);

                if (data.Get() != ':')
                    return 0;

                Whitespace(data);

                dynamic right = Expression(data);
                result = (result != 0) ? left : right;
            }
            return result;
        }

        public static bool Evaluate(Stream data)
        {
            dynamic result = Expression(data);

            Whitespace(data);

            if (!data.Eof())
                return Preprocessor.ThrowError(new CodeProcessorContext(new CodeProcessorConfig()), PreprocessorCodes.UnexpectedCharacter);

            return (result == 1);
        }
    }
}
