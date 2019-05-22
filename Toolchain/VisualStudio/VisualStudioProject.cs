// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SE.Forge.Systems;
using SE.Code.Build;
using SE.Storage;

namespace SE.Forge.VisualStudio
{
    public abstract class VisualStudioProject : BuildProject
    {
        public const string SharpGuid = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
        public const string CppGuid = "8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942";

        protected VisualStudioVersion version;

        public abstract Guid ProjectTypeGuid
        {
            get;
        }

        protected Guid projectGuid;
        public Guid ProjectGuid
        {
            get { return projectGuid; }
        }

        public VisualStudioVersion Version
        {
            get { return version; }
        }

        public IEnumerable<VisualStudioProject> DependencyProjects
        {
            get { return projectReferences.Where(x => x is VisualStudioProject).Cast<VisualStudioProject>(); }
        }

        protected VisualStudioDirectory parentDirectory;
        public VisualStudioDirectory ParentDirectory
        {
            get { return parentDirectory; }
            set { parentDirectory = value; }
        }

        protected OutputAssemblyType outputType;
        public OutputAssemblyType OutputType
        {
            get { return outputType; }
            set { outputType = value; }
        }

        protected string assemblyName;
        public string AssemblyName
        {
            get { return assemblyName; }
            set { assemblyName = value; }
        }

        bool isModule;
        public bool IsModule
        {
            get { return isModule; }
            set { isModule = value; }
        }

        public VisualStudioProject(VisualStudioVersion version, IEnumerable<FileDescriptor> files, PathDescriptor projectRoot, string name)
            : base(files, projectRoot, name)
        {
            this.version = version;
            LoadGuid();
        }
        public VisualStudioProject(VisualStudioVersion version, IEnumerable<FileDescriptor> files, PathDescriptor projectRoot, string name, params object[] args)
            : base(files, projectRoot, name, args)
        {
            this.version = version;
            LoadGuid();
        }

        void LoadGuid()
        {
            if (Exists())
                try
                {
                    XmlDocument tmp = new XmlDocument();
                    tmp.Load(GetAbsolutePath());

                    XmlNodeList nodes = tmp.GetElementsByTagName("ProjectGuid");
                    if (nodes.Count > 0)
                        projectGuid = Guid.ParseExact(nodes[0].InnerText.Trim('{', '}'), "D");
                }
                catch (Exception)
                { }

            if (projectGuid == Guid.Empty)
                projectGuid = Guid.NewGuid();
        }
    }
}
