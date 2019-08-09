// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Code.Analytics;
using SE.Forge.Systems;
using SE.Code.Build;
using SE.Threading;
using SE.Parallel;
using SE.Storage;
using SE.Text;
using SE.App;

namespace SE.Forge.VisualStudio
{
    public abstract class SharpProjectGenerator : Task, ITaskPrototype
    {
        class ToolTargetPlatform : TargetPlatform
        {
            public override string Name
            {
                get { return "Tools"; }
            }
            public override string GetTechnicalName(TargetArchitecture architecture)
            {
                return Name;
            }
            public override void SetDependentDefines(TargetArchitecture architecture)
            {
                
            }
        }

        private const string AnalyticsNodeName = "{0} C# Project '{1}'";
        private readonly static ToolTargetPlatform ToolsPlatform = new ToolTargetPlatform();
        private readonly static PathDescriptor ModulesPath;
        private readonly static PooledSpinLock builtStateCompleteLock = new PooledSpinLock();
        private readonly static Dictionary<Project, VisualSharpProject> builtStateComplete = new Dictionary<Project, VisualSharpProject>();
        protected readonly VisualStudioVersion version;

        public abstract bool Enabled
        {
            get;
        }

        public bool ExclusiveUse
        {
            get { return false; }
        }

        public override bool IsPending
        {
            get
            {
                LoadInput();
                if (inputPins[0].HasData)
                {
                    foreach (Project reference in (inputPins[0].Data as Project).References)
                        if (!reference.IsModule)
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

        static SharpProjectGenerator()
        {
            ModulesPath = new PathDescriptor(PathDescriptor.GetCommonParent(SharpAnalyzer.Analyzer.GetModulePaths()));
        }
        public SharpProjectGenerator(VisualStudioVersion version)
        {
            this.version = version;
            if (version != null)
            {
                inputPins = new TaskPin[]
                {
                    new ExclusivePin(this, typeof(Project), SystemTags.Sharp)
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
            VisualSharpProject csproj = new VisualSharpProject(version, project.Files.Nodes.Select(x => x.File), project.Location, project.Name);
            csproj.AssemblyName = project.AssemblyName;

            csproj.IsModule = project.IsModule;
            using(new Scope(builtStateCompleteLock))
                builtStateComplete.Add(project, csproj);

            foreach (BuildMode configuration in SharpAnalyzer.Analyzer.GetProjectConfigurations())
                csproj.BuildTargets.Add(new BuildTarget(ToolsPlatform, TargetArchitecture.x64, configuration));
            csproj.OutputType = project.AssemblyType;

            csproj.OutputPath = Application.GetDeploymentPath(project.Location);

            HashSet<Project> references = new HashSet<Project>();
            foreach (Project reference in project.References)
                AddReference(csproj, reference, references);

            foreach (FileDescriptor assembly in project.Externals)
                if (!csproj.Dependencies.Contains(assembly))
                     csproj.Dependencies.Add(assembly);

            Application.Log(SeverityFlags.Minimal, "Creating {0} project {1}", version.Flag.ToString(), csproj.FullName);

            SetBasicNamespace(project, csproj);
            csproj.CreateFile();

            outputPins[0].Data = csproj;
        }
        void AddReference(VisualSharpProject csproj, Project reference, HashSet<Project> references)
        {
            if (reference.IsModule)
            {
                if (!references.Contains(reference))
                {
                    references.Add(reference);

                    bool appDirectory = reference.Name == "App";
                    foreach (ReferenceNode node in reference.Files.Nodes)
                        if(appDirectory) csproj.Links.Add(node.File, node.File.GetRelativePath(reference.Location.Parent));
                        else csproj.Links.Add(node.File, node.File.GetRelativePath(ModulesPath.Parent));

                    foreach (Project tmp in reference.References)
                        AddReference(csproj, tmp, references);
                }
            }
            else if (!references.Contains(reference))
            {
                references.Add(reference);

                VisualSharpProject tmp; if(builtStateComplete.TryGetValue(reference, out tmp))
                    csproj.ProjectReferences.Add(tmp);
            }
        }
        void SetBasicNamespace(Project project, VisualSharpProject csproj)
        {
            Dictionary<string, int> gatherer = new Dictionary<string, int>();
            foreach (ReferenceNode node in project.Files.Nodes)
                foreach (KeyValuePair<int, string> @namespace in (node as SharpAnalyzer.SharpReferenceNode).Namespaces)
                {
                    string tmp = gatherer.Keys.Where(x => SharpAnalyzer.DependencyResolver.MatchesNamespace(x, @namespace.Value)).FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(tmp))
                        tmp = @namespace.Value;

                    int count; bool hasKey = gatherer.TryGetValue(tmp, out count);
                    count++;

                    if (hasKey) gatherer[tmp] = count;
                    else gatherer.Add(tmp, count);
                }

            csproj.RootNamespace = gatherer.Keys.MostCommon().Trim('.');
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
