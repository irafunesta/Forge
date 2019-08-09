// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Code.Analytics;
using SE.Forge.Systems;
using SE.Parallel;
using SE.Storage;
using SE.Config;
using SE.App;

namespace SE.Forge.SharpAnalyzer
{
    public class ProjectFence : VariadicTask, IConfigReceiver
    {
        private const string PrintProjectStackParameter = "PrintProjectStack";
        private const string ToolDisplayName = "C#";
        private const string AnalyticsNodeName = "Resolve Dependencies";

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
            foreach(TaskPin inputPin in inputPins)
                projects.Add(inputPin.Data as Project);

            Analyzer.GetModulePaths().ParallelFor((location) =>
            {
                foreach (TaskPin inputPin in inputPins)
                    if ((inputPin.Data as FileDescriptor).Location.Contains(location))
                        return;

                List<FileSystemDescriptor> files = Application.GetProjectFiles(location, ProjectLookup.Extensions);
                if (files.Count > 0 || files.Where(x => (x as FileDescriptor).Extension == "cs").Count() == 0)
                {
                    Project module = ProjectLookup.CreateProject(SystemTags.SharpModule, location, files);
                    module.AssemblyType = OutputAssemblyType.Library;
                    module.IsModule = true;

                    lock (projects)
                        if (!projects.Contains(module))
                        {
                            Application.Log(SeverityFlags.Full, "Loaded {0} module {1}", ToolDisplayName, module.Name);
                            projects.Add(module);
                        }
                }
            });
            projects.ParallelFor((project) =>
            {
                DependencyResolver.ConnectNamespaces(project, projects);
                DependencyResolver.ResolveNamespaces(project);
            });

            foreach (TaskPin inputPin in inputPins)
                outputPins.Add(new FlaggedPin(this, inputPin.Data, SystemTags.Sharp));

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
                foreach(Project project in projects)
                {
                    project.Files.Nodes.ParallelFor((file) =>
                    {
                        file.Rect = Rectangle.Empty;
                        file.Slot = null;
                    });
                    project.Files.Save(outputPath, string.Format("{0}.cs", project.Name));
                    AddProjectToGraph(moduleGraph, project);
                }
                moduleGraph.Save(outputPath, "SharpModules");
            }
        }
        void AddProjectToGraph(ReferenceGraph graph, Project project)
        {
            ProjectGraphNode root = (graph.Add<ProjectGraphNode>(project) as ProjectGraphNode);
            foreach(Project reference in project.References)
            {
                ProjectGraphNode child = graph.Nodes.Where(x => x.File == reference).FirstOrDefault() as ProjectGraphNode;
                if (child == null) child = (graph.Add<ProjectGraphNode>(root, reference) as ProjectGraphNode);
                else child.AddDependency(root);
            }
        }

        public override bool VariadicAccepts(TaskPin inputPin)
        {
            return (inputPin is FlaggedPin && inputPin.DataType == typeof(Project) && (inputPin as FlaggedPin).Flags == SystemTags.SharpSetup);
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
