// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Code.Analytics;
using SE.Code.Build;
using SE.Storage;
using SE.Text;
using SE.App;

namespace SE.Forge.Systems
{
    public class Project : FileDescriptor
    {
        UInt32 tag;
        public UInt32 Tag
        {
            get { return tag; }
        }

        string assemblyName;
        public string AssemblyName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(assemblyName))
                    lock(this)
                    {
                        if (string.IsNullOrWhiteSpace(assemblyName))
                            assemblyName = ProjectAnalyzer.GetAssemblyName(this);
                    }

                return assemblyName;
            }
        }

        ReferenceGraph files;
        public ReferenceGraph Files
        {
            get { return files; }
        }

        List<Project> references;
        public List<Project> References
        {
            get { return references; }
        }

        List<FileDescriptor> externals;
        public List<FileDescriptor> Externals
        {
            get { return externals; }
        }

        OutputAssemblyType assemblyType;
        public OutputAssemblyType AssemblyType
        {
            get { return assemblyType; }
            set { assemblyType = value; }
        }

        BuildTarget target;
        public BuildTarget Target
        {
            get { return target; }
            set { target = value; }
        }

        bool isModule;
        public bool IsModule
        {
            get { return isModule; }
            set { isModule = value; }
        }

        bool isCached;
        public bool IsCached
        {
            get { return isCached; }
            set { isCached = value; }
        }

        protected StorageDescriptor cache;
        public StorageDescriptor Cache
        {
            get 
            {
                if (cache == null)
                {
                    UInt32 tmp; if (target != null)
                        tmp = target.TechnicalName.Fnv32(tag);
                    else
                        tmp = tag;

                    PathDescriptor cacheDirectory = new PathDescriptor(Application.CacheDirectory, tmp.ToString());
                    if (!cacheDirectory.Exists())
                        cacheDirectory.Create();

                    cache = new StorageDescriptor(cacheDirectory, "{0}.cache", location.GetAbsolutePath().Fnv32());
                    cache.Load();
                }
                return cache;
            }
        }

        public Project(UInt32 tag, PathDescriptor location)
            :base(location, location.Name)
        {
            this.tag = tag;
            this.location = location;
            this.files = new ReferenceGraph();
            this.references = new List<Project>();
            this.externals = new List<FileDescriptor>();
        }
    }
}
