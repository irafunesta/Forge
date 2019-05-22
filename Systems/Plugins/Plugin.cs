// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using SE.Reflection;
using SE.Storage;
using SE.Config;
using SE.Text;
using SE.App;

namespace SE.Forge.Systems
{
    public class Plugin : Task
    {
        const string PluginCacheFileName = "{0}.cache";
        const string PluginAssemblyFileName = "{0}.plg";
        const string PluginAssemblyStorageAlias = "assembly";

        PathDescriptor path;
        public PathDescriptor Path
        {
            get { return path; }
        }

        public override TaskPin[] InputPins
        {
            get { return TaskPin.Empty; }
        }
        public override TaskPin[] OutputPins
        {
            get { return TaskPin.Empty; }
        }

        IPlugin instance;

        public Plugin(PathDescriptor path)
        {
            this.path = path;
            path.Equalize();
        }
        public void GetManualPage(PageFormatter page)
        {
            AutoConfig.GetAttributePage(instance, page);
        }

        void SetPluginFilter(Filter filter)
        {
            filter.Clear();
            FilterToken tkn = filter.Add(".*");
            tkn.Exclude = true;
            tkn = filter.Add(tkn, "*.cs");
        }
        FileDescriptor Compile(List<FileSystemDescriptor> files)
        {
            FileDescriptor outputAssembly = new FileDescriptor(Application.CacheDirectory, PluginAssemblyFileName, path.Name);
            SE.Forge.SharpCompiler.Compiler compiler = new SE.Forge.SharpCompiler.Compiler();
            compiler.Options.OutputType = Systems.OutputAssemblyType.Library;
            #if DEBUG
            compiler.Options.Debug = true;
            #endif
            compiler.Options.TargetFile = outputAssembly;

            foreach (FileDescriptor file in files)
                compiler.Options.Sources.Add(file.GetAbsolutePath());

            compiler.Options.Dependencies.Add("System.dll");
            compiler.Options.Dependencies.Add("System.Core.dll");
            compiler.Options.Dependencies.Add("Microsoft.CSharp.dll");

            List<KeyValuePair<int, string>> usings = new List<KeyValuePair<int, string>>();
            foreach (FileDescriptor file in files)
            {
                usings.Clear();
                SharpAnalyzer.CodeParser.GetUsingDirectives(file.GetText(Encoding.UTF8), usings);
                foreach (string dependency in usings.Select(x => x.Value))
                    if (dependency != "System")
                    {
                        List<FileDescriptor> assemblies; if (SharpAnalyzer.AssemblyResolver.TryGetAssemblies(dependency.Fnv32(), out assemblies))
                            foreach (FileDescriptor assembly in assemblies)
                                if (!compiler.Options.Dependencies.Contains(assembly.GetAbsolutePath()))
                                    compiler.Options.Dependencies.Add(assembly.GetAbsolutePath());
                    }
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (!assembly.IsDynamic && !assembly.GlobalAssemblyCache)
                    if (!compiler.Options.Dependencies.Contains(assembly.Location))
                        compiler.Options.Dependencies.Add(assembly.Location);

            compiler.Execute();
            return outputAssembly;
        }
        public override void Execute()
        {
            StorageDescriptor pluginCache = new StorageDescriptor(Application.CacheDirectory, PluginCacheFileName, path.Name);
            if (pluginCache.Load())
            {
                foreach (StorageContainer node in pluginCache.Nodes.Values)
                    if (!node.Validate())
                    {
                        pluginCache.Clear();
                        break;
                    }
            }
            StorageContainer assemblyNode; if (!pluginCache.TryGetNode(PluginAssemblyStorageAlias, out assemblyNode))
            {
                List<FileSystemDescriptor> files = Application.GetPluginFiles(path);
                if (files.Count > 0)
                {
                    FileDescriptor assemblyFile = Compile(files);
                    assemblyNode = pluginCache.Store(assemblyFile, PluginAssemblyStorageAlias);

                    foreach (FileDescriptor mixin in files)
                        pluginCache.Store(mixin);
                    pluginCache.Save();
                }
                else Application.Warning(SeverityFlags.None, "Plugin '{0}' has no code files to compile", path.Name);
                files = null;
            }
            if (assemblyNode != null && assemblyNode.Element.Exists())
            {
                Assembly assembly = Assembly.LoadFrom(assemblyNode.Element.GetAbsolutePath());
                Type type = assembly.GetType<IPlugin>();
                if (type != null)
                {
                    instance = type.CreateInstance<IPlugin>();

                    if (instance != null)
                    {
                        instance.Load();
                        return;
                    }
                }
                Application.Warning(SeverityFlags.None, "'{0}' is not a plugin", path.GetAbsolutePath());
            }
        }

        public void Release()
        {
            if (instance != null)
            {
                instance.Unload();
                instance = null;
            }
        }
    }
}
