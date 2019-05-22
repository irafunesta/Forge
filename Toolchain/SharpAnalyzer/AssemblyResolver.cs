// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using SE.Storage;
using SE.Text;

namespace SE.Forge.SharpAnalyzer
{
    public static partial class AssemblyResolver
    {
        private static string[] NetAssemblies = new string[]
        {
            "System.Data.dll",
            "System.Drawing.dll",
            "System.XML.dll",
            "System.Xml.Linq.dll",
            "System.Windows.Forms.dll"
        };
        private static string[] NamespaceBlacklist = new string[]
        {
            "System.Security.Cryptography"
        };

        private static Dictionary<UInt32, List<FileDescriptor>> assemblies = new Dictionary<UInt32, List<FileDescriptor>>();
        private static void LoadAssemblies()
        {
            PathDescriptor assemblyPath = new PathDescriptor(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
            List<FileDescriptor> assemblyFiles = new List<FileDescriptor>();
            foreach (string assembly in NetAssemblies)
                assemblyFiles.Add(new FileDescriptor(assemblyPath, assembly));

            for (int i = 0; i < assemblyFiles.Count; i++)
                LoadAssembly(assemblyFiles[i]);
        }
        public static void LoadAssembly(FileDescriptor assemblyFile)
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(assemblyFile.GetAbsolutePath());
                foreach (Type type in assembly.GetTypes())
                    if (!string.IsNullOrWhiteSpace(type.Namespace) && Array.IndexOf(NamespaceBlacklist, type.Namespace) == -1)
                    {
                        UInt32 key = type.Namespace.Fnv32();
                        List<FileDescriptor> result; if (!assemblies.TryGetValue(key, out result))
                        {
                            result = new List<FileDescriptor>();
                            assemblies.Add(key, result);
                        }
                        if (!result.Contains(assemblyFile))
                            result.Add(assemblyFile);
                    }
            }
            catch (ReflectionTypeLoadException)
            { }
            catch (BadImageFormatException)
            { }
        }

        static AssemblyResolver()
        {
            LoadAssemblies();
        }

        public static bool TryGetAssemblies(UInt32 id, out List<FileDescriptor> files)
        {
            return assemblies.TryGetValue(id, out files);
        }
        public static List<FileDescriptor> GetAssemblies(UInt32 id)
        {
            List<FileDescriptor> result;
            if (!TryGetAssemblies(id, out result))
                throw new IndexOutOfRangeException();

            return result;
        }
    }
}
