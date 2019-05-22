// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Code.Analytics;
using SE.Forge.Systems;
using SE.Parallel;
using SE.Storage;
using SE.Text;

namespace SE.Forge.SharpAnalyzer
{
    public static class DependencyResolver
    {
        public static bool MatchesNamespace(string declaration, string reference)
        {
            if (string.IsNullOrWhiteSpace(declaration))
                declaration = string.Empty;

            if (string.IsNullOrWhiteSpace(reference))
                reference = string.Empty;

            if (reference.StartsWith(declaration) || declaration.StartsWith(reference))
                return true;

            return false;
        }
        public static bool MatchesNamespace(List<KeyValuePair<int, string>> usings, string declaration, string reference)
        {
            if (declaration == reference) return true;
            foreach (KeyValuePair<int, string> @using in usings)
                if (@using.Value == reference)
                    return true;

            return MatchesNamespace(declaration, reference);
        }

        public static void ConnectNamespaces(Project project, IEnumerable<Project> projects)
        {
            project.Files.Nodes.ParallelFor((tmp) =>
            {
                ConnectNodes(project, (tmp as SharpReferenceNode), project, false);
            });
            project.Files.Nodes.ParallelFor((tmp) =>
            {
                foreach (Project reference in projects)
                    if (reference != project)
                        if (reference.AssemblyType == OutputAssemblyType.Library || reference.AssemblyType == OutputAssemblyType.Static)
                            ConnectNodes(project, (tmp as SharpReferenceNode), reference, true);
            });
        }
        public static void ConnectNodes(Project project, SharpReferenceNode node, Project graph, bool connectNamespaces)
        {
            foreach (KeyValuePair<int, string> reference in node.TypeReferences)
            {
                List<ReferenceNode> types; if (graph.Files.TryGetType(reference.Value, out types))
                {
                    KeyValuePair<int, string> @namespace = node.GetDeclaringNamespace(reference);
                    foreach (ReferenceNode rtmp in types)
                    {
                        bool isNamespaceRoot = !string.IsNullOrWhiteSpace(@namespace.Value) && !@namespace.Value.Contains(".");

                        SharpReferenceNode @ref = (rtmp as SharpReferenceNode);
                        foreach (KeyValuePair<int, string> nmsp in @ref.GetDeclaringNamespaces(reference.Value))
                        {
                            bool match; if (isNamespaceRoot) match = @namespace.Value == nmsp.Value;
                            else match = MatchesNamespace(@namespace.Value, nmsp.Value);
                            if (match)
                            {
                                node.AddDependency(@ref);
                                if (project != graph)
                                {
                                    lock (project)
                                        if (!project.References.Contains(graph))
                                            project.References.Add(graph);
                                }
                                continue;
                            }
                        }
                    }
                }
            }
            if (connectNamespaces)
                graph.Files.Nodes.ParallelFor((rtmp) =>
                {
                    bool connected;
                    SharpReferenceNode @ref = (rtmp as SharpReferenceNode);
                    foreach (KeyValuePair<int, string> @namespace in node.NamespaceReferences)
                    {
                        connected = false;
                        bool isNamespaceRoot = !@namespace.Value.Contains(".");
                        foreach (KeyValuePair<int, string> nmsp in @ref.Namespaces)
                        {
                            bool match; if (isNamespaceRoot) match = @namespace.Value == nmsp.Value;
                            else match = MatchesNamespace(@namespace.Value, nmsp.Value);
                            if (match)
                            {
                                connected = true;
                                node.AddDependency(@ref);
                                if (project != graph)
                                {
                                    lock (project)
                                        if (!project.References.Contains(graph))
                                            project.References.Add(graph);
                                }
                                break;
                            }
                        }
                        if (connected)
                            break;
                    }
                });
        }

        private static void ResolveNamespace(Project project, Project reference, HashSet<FileDescriptor> referenceAssemblies)
        {
            foreach (ReferenceNode tmp in reference.Files.Nodes)
                if (tmp is SharpReferenceNode)
                {
                    SharpReferenceNode node = (tmp as SharpReferenceNode);
                    foreach (KeyValuePair<int, string> @using in node.NamespaceReferences)
                        if (@using.Value != "System")
                        {
                            List<FileDescriptor> assemblies; if (SharpAnalyzer.AssemblyResolver.TryGetAssemblies(@using.Value.Fnv32(), out assemblies))
                                foreach (FileDescriptor assembly in assemblies)
                                    if (!referenceAssemblies.Contains(assembly))
                                    {
                                        project.Externals.Add(assembly);
                                        referenceAssemblies.Add(assembly);
                                    }
                        }
                }
        }
        public static void ResolveNamespaces(Project project)
        {
            HashSet<FileDescriptor> referenceAssemblies = new HashSet<FileDescriptor>();
            ResolveNamespace(project, project, referenceAssemblies);

            foreach (Project reference in project.References)
                ResolveNamespace(project, reference, referenceAssemblies);
        }
    }
}
