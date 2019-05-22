// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Storage;

namespace SE.Forge.VisualStudio
{
    public class VisualStudioDirectory
    {
        public const string DirectoryGuid = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

        protected Guid guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return guid; }
        }

        protected PathDescriptor path;
        public PathDescriptor Location
        {
            get { return path; }
        }

        public string Name
        {
            get { return path.Name; }
        }

        protected List<FileSystemDescriptor> files = new List<FileSystemDescriptor>();
        public List<FileSystemDescriptor> Files
        {
            get { return files; }
        }

        public VisualStudioDirectory(PathDescriptor path)
        {
            this.path = path;
        }
    }
}
