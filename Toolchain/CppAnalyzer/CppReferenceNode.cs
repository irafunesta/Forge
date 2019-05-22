// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using SE.Forge.CppPreprocessor.Tokens;
using SE.Forge.CppPreprocessor;
using SE.Code.Analytics;
using SE.Forge.Systems;
using SE.Code.Parsing;
using SE.Code.Build;
using SE.Storage;
using SE.Code;
using SE.Text;

namespace SE.Forge.CppAnalyzer
{
    public class CppReferenceNode : ReferenceNode
    {
        const string EntryPointExpression = @"(\bvoid\b|\bint\b)\s+\bmain\b\s*\((\s*\bint\b\s+\bargc\b\s*,\s*\bchar\b\s*\*\s*\bargv\b\s*\[\s*\])*\s*\)";
        const string WindowsEntryPointExpression = @"\bint\b\s+\bWINAPI\b\s+\bWinMain\b\s*\(\s*\bHINSTANCE\b\s+\bhInstance\b\s*,\s*\bHINSTANCE\b\s+ \bhPrevInstance\b\s*,\s*\bLPSTR\b\s+\bcmdLine\b\s*,\s*\bint\b\s+\bshowCmd\b\s*\)";

        private readonly static UInt32 ForgeId = "__forge__".Fnv32();

        private static Regex windowsEntryPointRegex = new Regex(WindowsEntryPointExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex entryPointRegex = new Regex(EntryPointExpression, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);
        private static CodeProcessorConfig config = new CodeProcessorConfig();

        BuildTarget target;
        public override string Text
        {
            get { return file.FullName; }
        }

        protected List<FileDescriptor> referenceFiles = new List<FileDescriptor>();
        public List<FileDescriptor> ReferenceFiles
        {
            get { return referenceFiles; }
        }

        protected List<string> fileLookups = new List<string>();
        public List<string> FileLookups
        {
            get { return fileLookups; }
        }

        static CppReferenceNode()
        {
            config.IgnoreTabs = true;
            config.IgnoreWhitespaces = true;

            config.Rules.Add(PreprocessorTokenizer.Direktive);
            config.Rules.Add(PreprocessorTokenizer.Any);
        }
        public CppReferenceNode(BuildTarget target)
            : base()
        {
            this.target = target;
        }
        public CppReferenceNode(BuildTarget target, FileDescriptor file)
            : base(file)
        {
            this.target = target;
        }

        public override void FetchExports()
        { }
        public override void FetchImports()
        {
            TokenStream result = new TokenStream();
            CodeProcessor pp = new CodeProcessor(config);
            using(FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                pp.Parse(fs, result);

            SyntaxTree<PreprocessorToken> tree = new SyntaxTree<PreprocessorToken>();
            if (Preprocessor.GetSyntaxTree(result, tree) && result.Eof())
            {
                result.Buffer.Clear();
                result.Position = 0;

                CodeProcessorContext context = new CodeProcessorContext(new CodeProcessorConfig());
                context.Definitions.Add(ForgeId, new Macro(ForgeId, "1"));

                foreach (KeyValuePair<string, object> define in target.Platform.Definitions)
                {
                    UInt32 id = define.Key.Fnv32();
                    context.Definitions.Add(id, new Macro(id, define.Value.ToString()));
                }
                foreach (KeyValuePair<string, object> define in target.Mode.Definitions)
                {
                    UInt32 id = define.Key.Fnv32();
                    context.Definitions.Add(id, new Macro(id, define.Value.ToString()));
                }

                context.File = file;
                IncludeParser.GetResult(context, tree.Root, referenceFiles, fileLookups, result);
            }
            else throw new CodeProcessorException(file.GetAbsolutePath(), result.Position);
            if (result.Length > 0)
            {
                Match match = entryPointRegex.Match(Encoding.UTF8.GetString(result.Buffer.ToArray()));
                if (match.Success)
                {
                    OutputAssemblyType type; if (string.IsNullOrWhiteSpace(match.Groups[1].Value)) type = OutputAssemblyType.Console;
                    else type = OutputAssemblyType.Executable;
                    if (type != OutputAssemblyType.Library)
                    {
                        flags.Add((UInt16)type);
                    }
                }
                else
                {
                    match = entryPointRegex.Match(Encoding.UTF8.GetString(result.Buffer.ToArray()));
                    if (match.Success)
                        flags.Add((UInt16)OutputAssemblyType.Executable);
                }
            }

            target = null;
        }
    }
}
