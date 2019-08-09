// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Code.Analytics;
using SE.Forge.Systems;
using SE.Threading;
using SE.Parallel;
using SE.Storage;
using SE.App;

namespace SE.Forge.SharpCompiler
{
    public partial class CompilerTask : Task, ITaskPrototype
    {
        private const string AnalyticsNodeName = "C# Compiler '{0}'";
        private readonly static PooledSpinLock builtStateCompleteLock = new PooledSpinLock();
        private readonly static HashSet<Project> builtStateComplete = new HashSet<Project>();

        public bool Enabled
        {
            get { return enabled; }
        }

        public bool ExclusiveUse
        {
            get { return false; }
        }

        public bool Display
        {
            get { return true; }
        }
        public override bool IsPending
        {
            get
            {
                LoadInput();
                if (inputPins[0].HasData)
                {
                    foreach (Project reference in (inputPins[0].Data as Project).References)
                        if (reference.AssemblyType == OutputAssemblyType.Library && !reference.IsModule)
                            using (new Scope(builtStateCompleteLock))
                                if (!builtStateComplete.Contains(reference))
                                    return true;

                    return false;
                }
                return true;
            }
        }

        TaskPin[] inputPins;
        public override TaskPin[] InputPins
        {
            get { return inputPins; }
        }

        TaskPin[] outputPins;
        public override TaskPin[] OutputPins
        {
            get { return outputPins; }
        }

        public CompilerTask()
        {
            inputPins = new TaskPin[]
            {
                new ExclusivePin(this, typeof(Project), SystemTags.Sharp)
            };
            outputPins = new TaskPin[]
            {
                new FlaggedPin(this, typeof(FileDescriptor), SystemTags.Sharp)
            };
        }

        public override void Execute()
        {
            LoadInput();
            Project project = inputPins[0].Data as Project;

            string extension;
            switch (project.AssemblyType)
            {
                case OutputAssemblyType.Console:
                case OutputAssemblyType.Executable:
                    extension = "exe";
                    break;
                default: 
                    extension = "dll";
                    break;
            }

            FileDescriptor outputAssembly = new FileDescriptor(Application.GetDeploymentPath(project.Location), "{0}.{1}", project.AssemblyName, extension);
            Compiler compiler = new Compiler();
            compiler.Options.OutputType = project.AssemblyType;
            compiler.Options.TargetFile = outputAssembly;

            if(Application.GenerateDebugInfo)
                compiler.Options.Debug = true;

            foreach (ReferenceNode node in project.Files.Nodes)
                compiler.Options.Sources.Add(node.File.GetAbsolutePath());

            compiler.Options.Dependencies.Add("System.dll");
            compiler.Options.Dependencies.Add("System.Core.dll");
            compiler.Options.Dependencies.Add("Microsoft.CSharp.dll");

            HashSet<Project> references = new HashSet<Project>();
            foreach (Project reference in project.References)
                AddReference(compiler, project, reference, references);

            foreach (FileDescriptor assembly in project.Externals)
                if (!compiler.Options.Dependencies.Contains(assembly.GetAbsolutePath()))
                     compiler.Options.Dependencies.Add(assembly.GetAbsolutePath());

            try
            {
                Application.Log(SeverityFlags.Minimal, "Compiling {0} project {1}", ToolDisplayName, project.Name);
                compiler.Execute();
            }
            finally
            {
                using(new Scope(builtStateCompleteLock))
                    builtStateComplete.Add(project);
            }
        }
        void AddReference(Compiler compiler, Project root, Project reference, HashSet<Project> references)
        {
            if (reference.IsModule)
            {
                if (!references.Contains(reference))
                {
                    references.Add(reference);

                    foreach (ReferenceNode node in reference.Files.Nodes)
                        compiler.Options.Sources.Add(node.File.GetAbsolutePath());

                    foreach (Project tmp in reference.References)
                        AddReference(compiler, root, tmp, references);
                }
            }
            else if (!compiler.Options.Dependencies.Contains(reference.GetAbsolutePath()))
                compiler.Options.Dependencies.Add(reference.GetAbsolutePath());
        }

        public bool VariadicAccepts(TaskPin inputPin)
        { 
            return false; 
        }
        public Task CreateInstance()
        {
            return new CompilerTask();
        }
        public override string ToString()
        {
            if (!inputPins[0].HasData) return base.ToString();
            else return string.Format(AnalyticsNodeName, (inputPins[0].Data as FileDescriptor).Name);
        }
    }
}
