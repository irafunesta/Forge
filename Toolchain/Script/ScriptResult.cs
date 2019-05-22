// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Storage;
using SE.Shell;

namespace SE.Forge.Script
{
    public struct ScriptResult
    {
        PathDescriptor[] projects;
        public PathDescriptor[] Projects
        {
            get { return projects; }
        }

        public ScriptResult(PathDescriptor location, Json.Document store)
        {
            Json.Node node = store.Root;
            if (node != null)
                node = node.Child;

            projects = new PathDescriptor[0];

            if (store == null) return;
            else while(node != null)
            {
                switch (node.Name.ToLowerInvariant())
                {
                    case "$projects":
                        {
                            string[] args = Store.TransformObject(node).Split(',');
                            projects = new PathDescriptor[args.Length];
                                
                            for(int i = 0; i < projects.Length; i++)
                                projects[i] = new PathDescriptor(location, args[i].Trim('"'));
                        }
                        break;
                }
                node = node.Next;
            }
        }
    }
}
