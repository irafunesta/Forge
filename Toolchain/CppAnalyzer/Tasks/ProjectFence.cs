// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Forge.CppCompiler;
using SE.Code.Analytics;
using SE.Forge.Systems;
using SE.Code.Build;
using SE.Parallel;
using SE.Storage;
using SE.Config;
using SE.Text;
using SE.App;

namespace SE.Forge.CppAnalyzer
{
    public class ProjectFence : VariadicTask, IConfigReceiver
    {
        class ReferenceContainer : StorageContainer
        {
            public override void UpdateHash()
            { }
        }

        private const string PrintProjectStackParameter = "PrintProjectStack";
        private const string ToolDisplayName = "C++";
        private const string AnalyticsNodeName = "Resolve Dependencies";

        private static readonly UInt32 ReferenceStorageTag = "reference".Fnv32();
        private static readonly UInt32 ExternalStorageTag = "external".Fnv32();

        [AutoConfig(PrintProjectStackParameter, Text = "Outputs profiling info from the project stack")]
        private static bool printProjectStack = false;

        List<TaskPin> outputPins;
        public override TaskPin[] OutputPins
        {
            get { return outputPins.ToArray(); }
        }

        public string DisplayName
        {
            get { return ToolDisplayName; }
        }
        public bool Display
        {
            get { return true; }
        }

        static ProjectFence()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<ProjectFence>(true, ref result);
        }
        public ProjectFence()
        {
            outputPins = new List<TaskPin>();
        }

        public override void Execute()
        {
            LoadInput();

            List<Project> projects = new List<Project>();
            foreach (TaskPin inputPin in inputPins)
                projects.Add(inputPin.Data as Project);

            Analyzer.GetModulePaths().ParallelFor((location) =>
            {
                foreach (TaskPin inputPin in inputPins)
                    if ((inputPin.Data as FileDescriptor).Location.Contains(location))
                        return;

                foreach (BuildTarget target in CompilerServices.Targets)
                {
                    Project module; if (!ProjectLookup.FromCache(target, SystemTags.Cpp, location, out module))
                    {
                        List<FileSystemDescriptor> files = Application.GetProjectFiles(location, ProjectLookup.Extensions);
                        if (files.Count > 0)
                            module = ProjectLookup.CreateProject(module, files);
                    }

                    module.AssemblyType = OutputAssemblyType.Static;
                    module.IsModule = true;

                    lock (projects)
                        if (!projects.Contains(module))
                            projects.Add(module);
                }
            });
            projects.ParallelFor((project) =>
            {
                StorageDescriptor cache = project.Cache;
                if (project.IsCached)
                {
                    cache.Nodes.Values.ParallelFor((element) =>
                    {
                        if (element.Tag == ReferenceStorageTag)
                        {
                            projects.ParallelFor((reference) =>
                            {
                                if (reference == element.Element as FileDescriptor)
                                {
                                    lock (project)
                                        project.References.Add(reference);
                                }
                            });
                        }
                        else if (element.Tag == ExternalStorageTag)
                            project.Externals.Add(element.Element as FileDescriptor);
                    });
                }
                else
                {
                    DependencyResolver.ConnectFiles(project, projects);

                    foreach (Project referene in project.References)
                        cache.Store<ReferenceContainer>(referene).Tag = ReferenceStorageTag;
                    foreach (FileDescriptor referene in project.Externals)
                        cache.Store<ReferenceContainer>(referene).Tag = ExternalStorageTag;

                    cache.Save();
                }
            });

            foreach (TaskPin inputPin in inputPins)
                outputPins.Add(new FlaggedPin(this, inputPin.Data, SystemTags.Cpp));

            for (int i = 0; i < inputPins.Count; i++)
                foreach (Project reference in (outputPins[i].Data as Project).References)
                    outputPins.Add(new FlaggedPin(this, reference, SystemTags.Cpp));

            if (printProjectStack)
            {
                PathDescriptor outputPath = Application.GetDeploymentPath(SystemTags.Analytics);
                if (!outputPath.Exists())
                    try
                    {
                        outputPath.Create();
                    }
                    catch { }

                ReferenceGraph moduleGraph = new ReferenceGraph();
                foreach (Project project in projects)
                {
                    project.Files.Nodes.ParallelFor((file) =>
                    {
                        file.Rect = Rectangle.Empty;
                        file.Slot = null;
                    });
                    project.Files.Save(outputPath, string.Format("{0}.cpp", project.Name));
                    AddProjectToGraph(moduleGraph, project);
                }
                moduleGraph.Save(outputPath, "CppModules");
            }
        }
        void AddProjectToGraph(ReferenceGraph graph, Project project)
        {
            ProjectGraphNode root = (graph.Add<ProjectGraphNode>(project) as ProjectGraphNode);
            foreach (Project reference in project.References)
            {
                ProjectGraphNode child = graph.Nodes.Where(x => x.File == reference).FirstOrDefault() as ProjectGraphNode;
                if (child == null) child = (graph.Add<ProjectGraphNode>(root, reference) as ProjectGraphNode);
                else child.AddDependency(root);
            }
        }

        public override bool VariadicAccepts(TaskPin inputPin)
        {
            return (inputPin is FlaggedPin && inputPin.DataType == typeof(Project) && (inputPin as FlaggedPin).Flags == SystemTags.CppSetup);
        }
        public override Task CreateInstance()
        {
            return new ProjectFence();
        }

        public void GetAdditionalManualInfo(PageFormatter page)
        { }
        public override string ToString()
        {
            return AnalyticsNodeName;
        }
    }
}
