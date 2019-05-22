// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Forge.Systems;
using SE.Code.Build;
using SE.Threading;
using SE.Storage;
using SE.Text;
using SE.App;

namespace SE.Forge.VisualStudio
{
    public abstract class CppProjectGenerator : Task, ITaskPrototype
    {
        private const string AnalyticsNodeName = "{0} Cpp Project '{1}'";
        private const string ProjectNamingPattern = "{0}_{1}";
        private readonly static PooledSpinLock builtStateCompleteLock = new PooledSpinLock();
        private readonly static Dictionary<Project, VisualCppProject> builtStateComplete = new Dictionary<Project, VisualCppProject>();
        protected readonly VisualStudioVersion version;

        public abstract bool Enabled
        {
            get;
        }
        public override bool IsPending
        {
            get
            {
                LoadInput();
                if (inputPins[0].HasData)
                {
                    foreach (Project reference in (inputPins[0].Data as Project).References)
                            using (new Scope(builtStateCompleteLock))
                                if (!builtStateComplete.ContainsKey(reference))
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

        public CppProjectGenerator(VisualStudioVersion version)
        {
            this.version = version;
            if (version != null)
            {
                inputPins = new TaskPin[]
                {
                    new ExclusivePin(this, typeof(Project), SystemTags.Cpp)
                };
                outputPins = new TaskPin[]
                {
                    new FlaggedPin(this, typeof(VisualStudioProject), version.Flag.ToString().Fnv32())
                };
            }
        }

        public override void Execute()
        {
            LoadInput();

            Project project = inputPins[0].Data as Project;
            VisualCppProject vcproj; if (CppCompiler.CompilerServices.Targets.Length == 1) vcproj = new VisualCppProject(version, project.Files.Nodes.Select(x => x.File), project.Location, project.Name);
            else vcproj = new VisualCppProject(version, project.Files.Nodes.Select(x => x.File), project.Location, string.Format(ProjectNamingPattern, project.Name, project.Target.TechnicalName));
            vcproj.AssemblyName = project.AssemblyName;

            vcproj.IsModule = project.IsModule;
            using(new Scope(builtStateCompleteLock))
                builtStateComplete.Add(project, vcproj);

            foreach(BuildMode configuration in CppAnalyzer.Analyzer.GetProjectConfigurations())
                vcproj.BuildTargets.Add(new BuildTarget(project.Target.Platform, project.Target.Architecture, configuration));
            vcproj.OutputType = project.AssemblyType;
            vcproj.OutputPath = Application.GetDeploymentPath(project.Location);

            HashSet<Project> references = new HashSet<Project>();
            foreach (Project reference in project.References)
                AddReference(vcproj, reference, references);

            foreach (FileDescriptor assembly in project.Externals)
                if (!vcproj.Dependencies.Contains(assembly))
                     vcproj.Dependencies.Add(assembly);

            vcproj.CreateFile();
            outputPins[0].Data = vcproj;
        }
        void AddReference(VisualCppProject vcproj, Project reference, HashSet<Project> references)
        {
            if (!references.Contains(reference))
            {
                references.Add(reference);

                foreach (Project @ref in reference.References)
                    AddReference(vcproj, @ref, references);

                if (reference.AssemblyType == OutputAssemblyType.Static)
                    foreach (FileDescriptor external in reference.Externals)
                        vcproj.Dependencies.Add(external);

                VisualCppProject tmp; if (builtStateComplete.TryGetValue(reference, out tmp))
                    vcproj.ProjectReferences.Add(tmp);
            }
        }

        public bool VariadicAccepts(TaskPin inputPin)
        { 
            return false;
        }
        public abstract Task CreateInstance();
        public override string ToString()
        {
            if (!inputPins[0].HasData) return base.ToString();
            else return string.Format(AnalyticsNodeName, version.Flag, (inputPins[0].Data as FileDescriptor).Name);
        }
    }
}
