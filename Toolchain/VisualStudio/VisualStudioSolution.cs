// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SE.Code.Build;
using SE.Storage;
using SE.Text;

namespace SE.Forge.VisualStudio
{
    public class VisualStudioSolution
    {
        protected VisualStudioVersion version;

        protected FileDescriptor file;
        public FileDescriptor File
        {
            get { return file; }
        }

        protected List<VisualStudioProject> projects = new List<VisualStudioProject>();
        public List<VisualStudioProject> Projects
        {
            get { return projects; }
        }

        protected List<VisualStudioDirectory> content = new List<VisualStudioDirectory>();
        public List<VisualStudioDirectory> Content
        {
            get { return content; }
        }

        protected List<BuildTarget> buildTargets = new List<BuildTarget>();

        public VisualStudioSolution(FileDescriptor file, VisualStudioVersion version)
		{
            this.file = file;
            this.version = version;
		}
        public VisualStudioSolution(FileDescriptor file)
            : this(file, null)
        {
            if (!VisualStudioUtils.FindEditorVersion(out version))
                throw new ArgumentOutOfRangeException("Visual Studio Target Version");
        }

        protected void SetFileHeader(StreamWriter writer)
        {
            switch (version.Flag)
            {
                case VisualStudioVersionFlag.VisualStudio2017:
                    {
                        writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                        writer.WriteLine("# Visual Studio 15");
                        writer.WriteLine("VisualStudioVersion = 15.0.25807.0");
                        writer.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
                    }
                    break;
                case VisualStudioVersionFlag.VisualStudio2015:
                    {
                        writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                        writer.WriteLine("# Visual Studio 14");
                        writer.WriteLine("VisualStudioVersion = 14.0.22310.1");
                        writer.WriteLine("MinimumVisualStudioVersion = 10.0.40219.1");
                    }
                    break;
                case VisualStudioVersionFlag.VisualStudio2013:
                    {
                        writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                        writer.WriteLine("# Visual Studio 2013");
                    }
                    break;
                case VisualStudioVersionFlag.VisualStudio2012:
                    {
                        writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
                        writer.WriteLine("# Visual Studio 2012");
                    }
                    break;
                case VisualStudioVersionFlag.VisualStudio2010:
                    {
                        writer.WriteLine("Microsoft Visual Studio Solution File, Format Version 11.00");
                        writer.WriteLine("# Visual Studio 2010");
                    }
                    break;
            }
        }
        protected void SetContentEntries(StreamWriter writer)
        {
            foreach (VisualStudioDirectory directory in content)
            {
                string contentDirectoryGuid = directory.Guid.ToString("B").ToUpperInvariant();
                writer.WriteLine("Project(\"{0}\") = \"{1}\", \"{1}\", \"{2}\"", VisualStudioDirectory.DirectoryGuid.ToGuid().ToString("B").ToUpperInvariant(), directory.Name, contentDirectoryGuid);
                if(directory.Files.Count > 0)
                {
                    writer.WriteLine("	ProjectSection(SolutionItems) = preProject");
                    foreach (FileDescriptor file in directory.Files)
                            writer.WriteLine("		{0} = {0}", file.GetRelativePath(directory.Location));
                    writer.WriteLine("	EndProjectSection");
                }
                writer.WriteLine("EndProject");
            }
        }
        protected void SetProjectEntries(StreamWriter writer)
        {
            foreach (VisualStudioProject project in projects)
            {
                writer.WriteLine("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"", project.ProjectTypeGuid.ToString("B").ToUpperInvariant(), project.Name, project.GetRelativePath(file), project.ProjectGuid.ToString("B").ToUpperInvariant());
                if (project.DependencyProjects.Count() > 0)
                {
                    writer.WriteLine("\tProjectSection(ProjectDependencies) = postProject");
                    foreach (VisualStudioProject dependency in project.DependencyProjects)
                        writer.WriteLine("\t\t{0} = {0}", dependency.ProjectGuid.ToString("B").ToUpperInvariant());
                    writer.WriteLine("\tEndProjectSection");
                }
                writer.WriteLine("EndProject");
            }
        }
        protected void SetAvailableConfigs(StreamWriter writer)
        {
            buildTargets.Clear();
            foreach (VisualStudioProject project in projects)
                foreach (BuildTarget target in project.BuildTargets)
                    if (!buildTargets.Contains(target))
                        buildTargets.Add(target);

            writer.WriteLine("	GlobalSection(SolutionConfigurationPlatforms) = preSolution");
            foreach (BuildTarget target in buildTargets)
            {
                string architectureString;
                switch (target.Architecture)
                {
                    case TargetArchitecture.arm:
                    case TargetArchitecture.x86:
                        architectureString = TargetArchitecture.x86.ToString();
                        break;

                    case TargetArchitecture.arm64:
                    case TargetArchitecture.x64:
                        architectureString = TargetArchitecture.x64.ToString();
                        break;

                    default:
                    case TargetArchitecture.Default:
                        architectureString = "Any CPU";
                        break;
                }

                //e.g. Debug|Win32 = Win32_Debug|x86
                writer.WriteLine("		{0}|{1} = {1}_{0}|{2}", target.Mode.Name, target.TechnicalName, architectureString);
            }
            writer.WriteLine("	EndGlobalSection");
        }
        protected void SetProjectConfigs(StreamWriter writer)
        {
            writer.WriteLine("	GlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (VisualStudioProject project in projects)
                foreach (BuildTarget target in buildTargets)
                {
                    string projectGuid = project.ProjectGuid.ToString("B").ToUpperInvariant();
                    string architectureString;
                    switch (target.Architecture)
                    {
                        case TargetArchitecture.arm:
                        case TargetArchitecture.x86:
                            architectureString = TargetArchitecture.x86.ToString();
                            break;

                        case TargetArchitecture.arm64:
                        case TargetArchitecture.x64:
                            architectureString = TargetArchitecture.x64.ToString();
                            break;

                        default:
                        case TargetArchitecture.Default:
                            architectureString = "Any CPU";
                            break;
                    }

                    // e.g. "{4232C52C-680F-4850-8855-DC39419B5E9B}.Debug|Win32.ActiveCfg = Win32_Debug|x86"
                    writer.WriteLine("		{0}.{1}|{2}.ActiveCfg = {2}_{1}|{3}", projectGuid, target.Mode.Name, target.TechnicalName, architectureString);

                    //could build on this platform
                    writer.WriteLine("		{0}.{1}|{2}.Build.0 = {2}_{1}|{3}", projectGuid, target.Mode.Name, target.TechnicalName, architectureString);

                    //could deploy on this platform
                    writer.WriteLine("		{0}.{1}|{2}.Deploy.0 = {2}_{1}|{3}", projectGuid, target.Mode.Name, target.TechnicalName, architectureString);
                }
            writer.WriteLine("	EndGlobalSection");
        }
        protected void SetDirectoryTree(StreamWriter writer)
        {
            writer.WriteLine("	GlobalSection(NestedProjects) = preSolution");
            foreach (VisualStudioProject project in projects)
                if (project.ParentDirectory != null)
                {
                    //	e.g. "{Item-GUID} = {Folder-GUID}"
                    writer.WriteLine("		{0} = {1}", project.ProjectGuid.ToString("B").ToUpperInvariant(), project.ParentDirectory.Guid.ToString("B").ToUpperInvariant());
                }
            writer.WriteLine("	EndGlobalSection");
        }
        protected void SetGlobals(StreamWriter writer)
        {
            writer.WriteLine("Global");

            SetAvailableConfigs(writer);
            SetProjectConfigs(writer);
            SetDirectoryTree(writer);
            
            writer.WriteLine("EndGlobal");
        }

        public void CreateFile()
        {
            using (FileStream fs = new FileStream(file.GetAbsolutePath(), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                StreamWriter sw = new StreamWriter(fs);

                SetFileHeader(sw);
                SetContentEntries(sw);
                SetProjectEntries(sw);
                SetGlobals(sw);

                sw.Flush();
                sw.Close();
            }
        }
    }
}
