// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.IO;
using SE.Forge.Systems;
using SE.Storage;

namespace SE.Forge.SharpCompiler
{
    public class CompilerOptions
    {
        CompilerParameters parameters;
        public CompilerParameters Parameters
        {
            get { return parameters; }
        }

        TempFileCollection temporaryFiles;
        public TempFileCollection TemporaryFiles
        {
            get { return temporaryFiles; }
        }

        public FileDescriptor TargetFile
        {
            get 
            {
                return new FileDescriptor
                (
                    new PathDescriptor(Path.GetDirectoryName(parameters.OutputAssembly)), 
                    Path.GetFileName(parameters.OutputAssembly)
                );
            }
            set { parameters.OutputAssembly = value.GetAbsolutePath(); }
        }

        OutputAssemblyType outputType;
        public OutputAssemblyType OutputType
        {
            get { return outputType; }
            set 
            { 
                outputType = value;
                parameters.GenerateExecutable = (value == OutputAssemblyType.Console || 
                                                 value == OutputAssemblyType.Executable);
            }
        }

        bool debugFlag;
        public bool Debug
        {
            get { return debugFlag; }
            set { debugFlag = value; }
        }

        public IList Dependencies
        {
            get { return parameters.ReferencedAssemblies; }
        }

        List<string> sources = new List<string>();
        public IList Sources
        {
            get { return sources; }
        }

        Dictionary<string, string> defines = new Dictionary<string, string>();
        public Dictionary<string, string> Defines
        {
            get { return defines; }
        }

        public bool WarningAsError
        {
            get { return parameters.TreatWarningsAsErrors; }
            set { parameters.TreatWarningsAsErrors = value; }
        }

        public CompilerOptions()
        {
            temporaryFiles = new TempFileCollection();
            parameters = new CompilerParameters();
            parameters.TempFiles = temporaryFiles;
            parameters.GenerateInMemory = false;
            parameters.WarningLevel = 4;
        }

        public CompilerParameters Finalize()
        {
            if (defines.Count > 0)
            {
                parameters.CompilerOptions += " /define:";
                bool first = true; foreach (string define in defines.Keys)
                {
                    if (!first) parameters.CompilerOptions += ";";
                    parameters.CompilerOptions += define;
                }
            }

            parameters.IncludeDebugInformation = debugFlag;
            if(!debugFlag)
                parameters.CompilerOptions += " /optimize";

            if (outputType == OutputAssemblyType.Executable)
                parameters.CompilerOptions += " /target:winexe";

            return parameters;
        }
    }
}
