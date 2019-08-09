// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Threading;
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
    public class ProjectLookup : ProjectTask, IConfigReceiver
    {
        private const string IgnoreErrorsParameter = "IgnoreErrors";
        private const string ToolDisplayName = "C++";
        private const string AnalyticsNodeName = "C++ Project '{0}'";

        private static readonly UInt32 LibraryStorageTag = "library".Fnv32();
        private static readonly UInt32 ConsoleStorageTag = "console".Fnv32();
        private static readonly UInt32 ExeStorageTag = "exe".Fnv32();

        [AutoConfig(IgnoreErrorsParameter, Text = "Logs #error directives instead of throwing")]
        private static bool ignoreErrors = false;
        public static bool IgnoreErrors
        {
            get { return ignoreErrors; }
        }

        public readonly static string[] Extensions = new string[]
            {
                "c",
                "cpp",
                "h",
                "hpp"
            };
        public override string[] FileExtensions
        {
            get { return Extensions; }
        }

        public string DisplayName
        {
            get { return ToolDisplayName; }
        }
        public bool Display
        {
            get { return true; }
        }

        static ProjectLookup()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<ProjectLookup>(true, ref result);
        }
        public ProjectLookup()
            : base(SystemTags.CppSetup)
        {
            outputPins = new TaskPin[Math.Max(CompilerServices.Targets.Length, 1)];
            for (int i = 0; i < outputPins.Length; i++)
                outputPins[i] = new FlaggedPin(this, typeof(Project), SystemTags.CppSetup);
        }

        public override void Execute()
        {
            LoadInput();

            try
            {
                if (!(inputPins[0].Data as PathDescriptor).Exists())
                {
                    string name = (inputPins[0].Data as PathDescriptor).Name;
                    ((Action)(() =>
                    {
                        Application.Error(SeverityFlags.None, "Directory '{0}' not found", name);

                    })).Once(name.Fnv32());

                    Cancel();
                    return;
                }

                bool isDefault; if (CompilerServices.Targets.Length == 0 && CompilerServices.SetDefaultTarget())
                    isDefault = true;
                else
                    isDefault = false;

                for (int i = 0; i < CompilerServices.Targets.Length; i++)
                {
                    Project project; if (!FromCache(CompilerServices.Targets[i], SystemTags.Cpp, inputPins[0].Data as PathDescriptor, out project))
                    {
                        List<FileSystemDescriptor> files = Application.GetProjectFiles(inputPins[0].Data as PathDescriptor, Extensions);
                        if (files.Count > 0)
                        {
                            if (isDefault)
                                ((Action)(() =>
                                {
                                    Application.Warning(SeverityFlags.None, "C++ build target missing. Selecting {0} as target", CompilerServices.Targets[0].TechnicalName);

                                })).Once(CompilerServices.Targets[0].TechnicalName.Fnv32());

                            project = CreateProject(project, files);
                            Application.Log(SeverityFlags.Full, "Loaded {0} project {1}::{2}", ToolDisplayName, project.Name, project.Target.TechnicalName);
                            outputPins[i].Data = project;
                        }
                        else Cancel();
                    }
                    else
                    {
                        if (isDefault)
                            ((Action)(() =>
                            {
                                Application.Warning(SeverityFlags.None, "C++ build target missing. Selecting {0} as target", CompilerServices.Targets[0].TechnicalName);

                            })).Once(CompilerServices.Targets[0].TechnicalName.Fnv32());

                        Application.Log(SeverityFlags.Full, "Cached {0} project {1}::{2}", ToolDisplayName, project.Name, project.Target.TechnicalName);
                        outputPins[i].Data = project;
                    }
                }
            }
            catch (Exception e)
            {
                Cancel();
                throw e;
            }
        }
        protected override Project CreateProject(object arguments, List<FileSystemDescriptor> files)
        {
            throw new NotImplementedException();
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

        public static bool FromCache(BuildTarget target, UInt32 tag, PathDescriptor location, out Project project)
        {
            Project tmp = new Project(tag, location);
            tmp.AssemblyType = OutputAssemblyType.Static;
            tmp.Target = target;
            project = tmp;

            if (Application.Rebuild)
                return false;

            PathDescriptor cacheDirectory = new PathDescriptor(Application.CacheDirectory, tag.ToString());
            if (!cacheDirectory.Exists())
                return false;

            StorageDescriptor cache = tmp.Cache;
            if (cache.Nodes.Count > 0)
            {
                Int32 rebuildFlag = 0;
                cache.Nodes.Values.ParallelFor((node) =>
                {
                    if (Interlocked.CompareExchange(ref rebuildFlag, 0, 0) == 0)
                    {
                        switch (node.Tag)
                        {
                            case 0: break;
                            default:
                                {
                                    if (node.Tag == LibraryStorageTag)
                                        lock (tmp)
                                            tmp.AssemblyType = OutputAssemblyType.Library;
                                    else if (node.Tag == ConsoleStorageTag)
                                        lock (tmp)
                                            tmp.AssemblyType = OutputAssemblyType.Console;
                                    else if (node.Tag == ExeStorageTag)
                                        lock (tmp)
                                            tmp.AssemblyType = OutputAssemblyType.Executable;
                                    else return;
                                }
                                break;
                        }
                        if (node.Validate())
                        {
                            if (node.Element is FileDescriptor)
                                lock (tmp)
                                    tmp.Files.Nodes.Add(new CppReferenceNode(target, node.Element as FileDescriptor));
                        }
                        else Interlocked.Exchange(ref rebuildFlag, 1);
                    }
                });
                if (rebuildFlag == 0)
                {
                    project.IsCached = true;
                    return true;
                }
            }
            project.Files.Clear();
            project.AssemblyType = OutputAssemblyType.Library;
            return false;
        }

        public static Project CreateProject(Project project, List<FileSystemDescriptor> files)
        {
            StorageDescriptor cache = project.Cache;
            files.ParallelFor((file) =>
            {
                CppReferenceNode node = new CppReferenceNode(project.Target, file as FileDescriptor);
                node.FetchImports();

                lock (project)
                    project.Files.Nodes.Add(node);

                StorageContainer element = cache.Store(file);
                if (node.Flags.Count != 0)
                {
                    switch ((OutputAssemblyType)node.Flags[0])
                    {
                        case OutputAssemblyType.Library: element.Tag = LibraryStorageTag; break;
                        case OutputAssemblyType.Console: element.Tag = ConsoleStorageTag; break;
                        case OutputAssemblyType.Executable: element.Tag = ExeStorageTag; break;
                    }
                    lock (project)
                        project.AssemblyType = (OutputAssemblyType)node.Flags[0];
                }
            });

            ProjectAnalyzer.AddProjectFiles(project);

            Filter filter = new Filter();
            filter.Add(".*").Exclude = true;

            project.Location.FindDirectories(filter).ParallelFor((directory) =>
            {
                cache.Store(directory);
            });

            Analyzer.SetProjectType(project);
            Analyzer.SetProjectDependencies(project);
            return project;
        }

        public void GetAdditionalManualInfo(PageFormatter page)
        {
            List<UInt32> ids = new List<UInt32>();
            foreach (ICppCompilerService service in CompilerServices.Services)
                foreach (TargetArchitecture architecture in service.Architectures)
                    foreach (TargetPlatform platform in service.Platforms)
                    {
                        UInt32 id = platform.GetTechnicalName(architecture).Fnv32();
                        if (!ids.Contains(id))
                        {
                            page.AddRow(string.Format("-{0}", platform.GetTechnicalName(architecture)), string.Format("Enables {0} {1} platform handling", architecture, platform.Name));
                            ids.Add(id);
                        }
                    }
        }
    }
}
