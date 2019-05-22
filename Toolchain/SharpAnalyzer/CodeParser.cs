// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SE.Forge.Systems;

namespace SE.Forge.SharpAnalyzer
{
    public static class CodeParser
    {
        private readonly static string[] Keywords = new string[]
        {
            "abstract",
            "as",
            "async",
            "await",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "dynamic",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "partial",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "var",
            "virtual",
            "void",
            "volatile",
            "while",
            "yield"
        };

        const string BlockCommentExpression = @"/\*(.*?)\*/";
        const string LineCommentExpression = @"//(.*?)\r?\n";
        const string StringExpression = @"""((\\[^\n]|[^""\n])*)""";
        const string VerbatimStringExpression = @"@(""[^""]*"")+";
        const string EntryPointExpression = @"(\[\s*\bSTAThread\b\s*\]\s*[\n\r]*\s*)*(?:\bpublic\b\s+)*\bstatic\b\s+(\bvoid\b|\bint\b)\s+\bMain\b\s*\((\s*\bstring\b\s*\[\s*\]\s+\bargs\b)*\s*\)";
        const string VariableDelcarationExpression = @"\b((?!
          \babstract\b|
          \bas\b|
          \basync\b|
          \bawait\b|
          \bbase\b|
          \bbool\b|
          \bbreak\b|
          \bbyte\b|
          \bcase\b|
          \bcatch\b|
          \bchar\b|
          \bchecked\b|
          \bclass\b|
          \bconst\b|
          \bcontinue\b|
          \bdecimal\b|
          \bdefault\b|
          \bdelegate\b|
          \bdo\b|
          \bdouble\b|
          \bdynamic\b|
          \belse\b|
          \benum\b|
          \bevent\b|
          \bexplicit\b|
          \bextern\b|
          \bfalse\b|
          \bfinally\b|
          \bfixed\b|
          \bfloat\b|
          \bfor\b|
          \bforeach\b|
          \bgoto\b|
          \bif\b|
          \bimplicit\b|
          \bin\b|
          \bint\b|
          \binterface\b|
          \binternal\b|
          \bis\b|
          \block\b|
          \blong\b|
          \bnamespace\b|
          \bnew\b|
          \bnull\b|
          \bobject\b|
          \boperator\b|
          \bout\b|
          \boverride\b|
          \bparams\b|
          \bpartial\b|
          \bprivate\b|
          \bprotected\b|
          \bpublic\b|
          \breadonly\b|
          \bref\b|
          \breturn\b|
          \bsbyte\b|
          \bsealed\b|
          \bshort\b|
          \bsizeof\b|
          \bstackalloc\b|
          \bstatic\b|
          \bstring\b|
          \bstruct\b|
          \bswitch\b|
          \bthis\b|
          \bthrow\b|
          \btrue\b|
          \btry\b|
          \btypeof\b|
          \buint\b|
          \bulong\b|
          \bunchecked\b|
          \bunsafe\b|
          \bushort\b|
          \busing\b|
          \bvar\b|
          \bvirtual\b|
          \bvoid\b|
          \bvolatile\b|
          \bwhile\b|
          \byield\b
          )[a-zA-Z_][a-zA-Z0-9_]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*((\[[a-zA-Z0-9_]*\]|\<[a-zA-Z_][a-zA-Z0-9_]*\>)\s*)*[\[;,=)]";
        const string UsingExpression = @"\busing\b\s*([a-zA-Z_][.a-zA-Z0-9_]*)\s*[\;]";
        const string NamespaceExpression = @"\bnamespace\b\s*([a-zA-Z_][.a-zA-Z0-9_]*)";
        const string TypeUsageExpression = @"\b([a-zA-Z_][a-zA-Z0-9_]*\.\s*)+([a-zA-Z_][a-zA-Z0-9_]*)\s*[\[;,=\(<>)]";
        const string InheritanceExpression = @"[\:|\,|\(]\s+\b([a-zA-Z_][.a-zA-Z0-9_]*)\s";
        const string TypeDeclarationExpression = @"\b(?:\binterface\b|\bclass\b|\bstruct\b|\benum\b)\s+([a-zA-Z_][a-zA-Z0-9_]*)";

        private static Regex blockCommentRegex = new Regex(BlockCommentExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex lineCommentRegex = new Regex(LineCommentExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex stringRegex = new Regex(StringExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex verbatimStringRegex = new Regex(VerbatimStringExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex entryPointRegex = new Regex(EntryPointExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex variableDelcarationRegex = new Regex(VariableDelcarationExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex usingRegex = new Regex(UsingExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex namespaceRegex = new Regex(NamespaceExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex typeUsageRegex = new Regex(TypeUsageExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex inheritanceRegex = new Regex(InheritanceExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex typeDeclarationRegex = new Regex(TypeDeclarationExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);

        public static string Prepare(string data)
        {
            data = blockCommentRegex.Replace(data, string.Empty);
            data = lineCommentRegex.Replace(data, string.Empty);
            data = stringRegex.Replace(data, string.Empty);
            data = verbatimStringRegex.Replace(data, string.Empty);
            return data;
        }

        public static string GetNodeType(string data, ref OutputAssemblyType type)
        {
            Match match = entryPointRegex.Match(data);
            if (match.Success)
            {
                if (string.IsNullOrWhiteSpace(match.Groups[1].Value)) type = OutputAssemblyType.Console;
                else type = OutputAssemblyType.Executable;

                return entryPointRegex.Replace(data, "*");
            }
            else return data;
        }

        private static string ProcessRegex(Regex regex, string data, List<KeyValuePair<int, string>> references)
        {
            MatchCollection matches = regex.Matches(data);
            foreach (Match match in matches)
            {
                string value = match.Groups[1].Value;
                value = value.Trim().TrimEnd('.');

                KeyValuePair<int, string> pair = new KeyValuePair<int, string>(match.Index, value);
                if (Array.IndexOf(Keywords, value) == -1)
                    references.Add(pair);
            }
            return regex.Replace(data, "*");
        }

        public static string GetDeclarations(string data, List<KeyValuePair<int, string>> namespaces, List<KeyValuePair<int, string>> declarations)
        {
            data = ProcessRegex(namespaceRegex, data, namespaces);
            data = ProcessRegex(typeDeclarationRegex, data, declarations);

            return data;
        }
        public static string GetUsingDirectives(string data, List<KeyValuePair<int, string>> namespaces)
        {
            data = ProcessRegex(usingRegex, data, namespaces);
            return data;
        }
        public static string GetReferences(string data, List<KeyValuePair<int, string>> types)
        {
            data = ProcessRegex(variableDelcarationRegex, data, types);
            data = ProcessRegex(typeUsageRegex, data, types);
            data = ProcessRegex(inheritanceRegex, data, types);

            return data;
        }
    }
}
