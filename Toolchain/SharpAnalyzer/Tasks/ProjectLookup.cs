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
using SE.App;

namespace SE.Forge.SharpAnalyzer
{
    public class ProjectLookup : ProjectTask
    {
        private const string AnalyticsNodeName = "C# Project '{0}'";
        public readonly static string[] Extensions = new string[]
            {
                "cs",
                "resx"
            };
        public override string[] FileExtensions
        {
            get { return Extensions; }
        }

        public ProjectLookup()
            :base(SystemTags.SharpSetup)
        { }

        public override void Execute()
        {
            LoadInput();

            try
            {
                List<FileSystemDescriptor> files = Application.GetProjectFiles(inputPins[0].Data as PathDescriptor, Extensions);
                if (files.Count > 0 && files.Where(x => (x as FileDescriptor).Extension == "cs").Count() != 0)
                {
                    outputPins[0].Data = CreateProject(null, files);
                }
                else Cancel();
            }
            catch (Exception e)
            {
                Cancel();
                throw e;
            }
        }

        protected override Project CreateProject(object arguments, List<FileSystemDescriptor> files)
        {
            return CreateProject(SystemTags.Sharp, inputPins[0].Data as PathDescriptor, files);
        }

        public override Task CreateInstance()
        {
            return new ProjectLookup();
        }
        public override string ToString()
        {
            if (!inputPins[0].HasData) return base.ToString();
            else return string.Format(AnalyticsNodeName, (inputPins[0].Data as PathDescriptor).Name);
        }

        public static Project CreateProject(UInt32 tag, PathDescriptor location, List<FileSystemDescriptor> files)
        {
            Project project = new Project(tag, location);
            files.ParallelFor((file) =>
            {
                SharpReferenceNode node = new SharpReferenceNode(file as FileDescriptor);
                node.FetchExports();
                lock(project)
                {
                    project.Files.Nodes.Add(node);
                    foreach (KeyValuePair<int, string> type in node.Declarations)
                        project.Files[type.Value].Add(node);
                }
                if (node.Flags.Count != 0)
                    lock(project)
                        project.AssemblyType = (OutputAssemblyType)node.Flags[0];
            });

            ProjectAnalyzer.AddProjectFiles(project);
            return project;
        }
    }
}
