// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Code.Analytics;
using SE.Forge.Systems;
using SE.Parallel;
using SE.Storage;
using SE.Text;

namespace SE.Forge.CppAnalyzer
{
    public static class DependencyResolver
    {
        public static void ConnectFiles(Project project, IEnumerable<Project> projects)
        {
            project.Files.Nodes.ParallelFor((tmp) =>
            {
                foreach (FileDescriptor file in (tmp as CppReferenceNode).ReferenceFiles)
                {
                    ReferenceNode @ref = project.Files.Nodes.FirstOrDefault(x => x.File == file);
                    if (@ref != null)
                        tmp.AddDependency(@ref);
                }
                ConnectNodes(project, (tmp as CppReferenceNode), project, false);
            });
            project.Files.Nodes.ParallelFor((tmp) =>
            {
                foreach (Project reference in projects)
                    if (reference != project)
                        if (reference.AssemblyType == OutputAssemblyType.Library || reference.AssemblyType == OutputAssemblyType.Static)
                            ConnectNodes(project, (tmp as CppReferenceNode), reference, true);
            });
        }
        public static void ConnectNodes(Project project, CppReferenceNode node, Project graph, bool connectNamespaces)
        {
            for (int i = 0; i < node.FileLookups.Count; i++)
            {
                string lookup = node.FileLookups[i];
                FileDescriptor file = FileDescriptor.Create(graph.Location, lookup);
                ReferenceNode @ref = graph.Files.Nodes.FirstOrDefault(x => x.File == file);
                if (@ref != null)
                {
                    node.AddDependency(@ref);
                    if (project != graph)
                    {
                        lock (project)
                        {
                            if (!project.References.Contains(graph))
                                project.References.Add(graph);

                            node.FileLookups.RemoveAt(i);

                            i--;
                            continue;
                        }
                    }
                }
            }
        }
    }
}
