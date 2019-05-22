// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Storage;

namespace SE.Forge.VisualStudio
{
    public class VisualStudioVersion
    {
        private static readonly string[] versions = new string[] 
            { 
                "15.0", 
                "14.0", 
                "12.0", 
                "11.0", 
                "10.0" 
            };

        protected string version;
        public string Version
        {
            get { return version; }
        }

        public VisualStudioVersionFlag Flag
        {
            get 
            {
                int index = Array.IndexOf(versions, version);
                if (index < 0) return VisualStudioVersionFlag.Invalid;
                return (VisualStudioVersionFlag)(index + 1);
            }
        }

        protected PathDescriptor path;
        public PathDescriptor Path
        {
            get { return path; }
        }

        public VisualStudioVersion(string versionString, PathDescriptor path)
        {
            this.path = path;
            this.version = versionString;
        }

        public override string ToString()
        {
            return Flag.ToString();
        }
    }
}
