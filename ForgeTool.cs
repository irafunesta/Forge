// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using SE.Forge.Systems.Profiles;
using SE.Forge.Systems;
using SE.Reflection;
using SE.Threading;
using SE.Parallel;
using SE.Storage;
using SE.Mixins;
using SE.Config;
using SE.Text;
using SE.App;

namespace SE.Forge
{
    public static partial class ForgeTool
    {
        private static List<Plugin> plugins = new List<Plugin>();

        const string ModuleCacheFileName = "Mixins.cache";
        const string MixinAssemblyFileName = "Mixins.mx";
        const string MixinAssemblyStorageAlias = "assembly";

        private readonly static Type MixinType = typeof(Mixin);

        private static void CacheCleanup()
        {
            if (!Application.CacheDirectory.Exists())
                Application.CacheDirectory.Create();
            else
            {
                if (Application.ClearCache)
                    Application.CacheDirectory.Clear();

                #if !DEBUG
                DateTime creationTime = Application.Timestamp;
                foreach (FileDescriptor file in Application.CacheDirectory.GetFiles())
                    if (creationTime < file.Timestamp)
                        file.Delete();
                #endif
            }
        }

        private static void SetMixinFilter(Filter filter)
        {
            filter.Clear();
            FilterToken tkn = filter.Add(".*");
            tkn.Exclude = true;
            tkn = filter.Add(tkn, "*.Build.cs");
        }
        private static FileDescriptor Compile(List<FileSystemDescriptor> files)
        {
            FileDescriptor outputAssembly = new FileDescriptor(Application.CacheDirectory, MixinAssemblyFileName);
            SE.Forge.SharpCompiler.Compiler compiler = new SE.Forge.SharpCompiler.Compiler();
            compiler.Options.OutputType = Systems.OutputAssemblyType.Library;
            #if DEBUG
            compiler.Options.Debug = true;
            #endif
            compiler.Options.TargetFile = outputAssembly;

            foreach (FileDescriptor file in files)
                compiler.Options.Sources.Add(file.GetAbsolutePath());

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (!assembly.IsDynamic)
                    compiler.Options.Dependencies.Add(assembly.Location);

            compiler.Execute();
            return outputAssembly;
        }
        private static void LoadMixins()
        {
            StorageDescriptor mixinCache = new StorageDescriptor(Application.CacheDirectory, ModuleCacheFileName);
            if (mixinCache.Load())
            {
                foreach (StorageContainer node in mixinCache.Nodes.Values)
                    if (!node.Validate())
                    {
                        mixinCache.Clear();
                        break;
                    }
            }
            StorageContainer assemblyNode; if (!mixinCache.TryGetNode(MixinAssemblyStorageAlias, out assemblyNode))
            {
                Filter filter = new Filter();

                SetMixinFilter(filter);
                List<FileSystemDescriptor> mixins = Application.SdkDirectory.FindFiles(filter);
                if (mixins.Count > 0)
                {
                    FileDescriptor assemblyFile = Compile(mixins);
                    assemblyNode = mixinCache.Store(assemblyFile, MixinAssemblyStorageAlias);

                    foreach (FileDescriptor mixin in mixins)
                        mixinCache.Store(mixin);
                    mixinCache.Save();
                }
                filter = null;
                mixins = null;
            }
            MixinManager.RegisterMixins(Assembly.GetExecutingAssembly());
            if (assemblyNode != null && assemblyNode.Element.Exists())
                MixinManager.RegisterMixins(Assembly.LoadFrom(assemblyNode.Element.GetAbsolutePath()));
        }

        private static void Start()
        {
            List<Task> tasks = new List<Task>();
            ForgeSetup setup = new ForgeSetup();
            foreach (PathDescriptor path in Application.GetPluginList())
            {
                Plugin plugin = new Plugin(path);
                plugin.Child = setup;
                plugins.Add(plugin);
                tasks.Add(plugin);
            }
            if (tasks.Count == 0)
                tasks.Add(setup);

            TaskGraph.AwaitCompletion(tasks);
            Application.ProcessLogMessages();
        }
        public static int Main(string[] args)
        {
            if (args == null || args.Length == 0)
                args = new string[] { "-?" };

            try
            {
                Application.Shutdown += ThreadPool.Unload;
                AutoConfigResult result = Application.LoadManifest(args);
                if (result.Errors.Count > 0)
                {
                    foreach (string error in result.Errors)
                        Application.Error(error);

                    return Application.FailureReturnCode;
                }

                ThreadPool.ExecutionException += new System.Threading.ThreadExceptionEventHandler(ThreadPool_ExecutionException);

                CacheCleanup();
                LoadMixins();

                ThreadPool.LowPriorityCapacity *= 4;
                TaskGraph.SetEndPoints(new KeyValuePair<Parallel.Adapter, Action<Task>>[]
                {
                    new KeyValuePair<Parallel.Adapter, Action<Task>>
                    (
                        new Parallel.Adapter(new Parallel.PoolingBehavior()),
                        LocalProfile.ProcessLocalTask
                    )
                });

                Start();

                Application.Profile.Dispose();
                foreach (Plugin plugin in plugins)
                    plugin.Release();

                if (TaskGraph.Errors > 0) return Application.FailureReturnCode;
                else return Application.SuccessReturnCode;
            }
            #if !DEBUG
            catch (Exception er)
            {
                if (Application.LogSeverity > SeverityFlags.Minimal) Application.Error(SeverityFlags.None, "{0}{1}{2}", er.Message, Environment.NewLine, er.StackTrace);
                else Application.Error(SeverityFlags.None, er.Message);
                return Application.FailureReturnCode;
            }
            #endif
            finally
            {
                Application.Unload();
            }
        }

        private static void ThreadPool_ExecutionException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            if (Application.LogSeverity > SeverityFlags.Minimal) Application.Error(SeverityFlags.None, "{0}{1}{2}", e.Exception.Message, Environment.NewLine, e.Exception.StackTrace);
            else Application.Error(SeverityFlags.None, e.Exception.Message);
        }

        public static void DisplayManual()
        {
            Application.Log(SeverityFlags.None, "Forge {0}, distributed under the Schroedinger Entertainment EULA (See EULA.md for details)", Application.Version);

            StorageDescriptor mixinCache = new StorageDescriptor(Application.CacheDirectory, ModuleCacheFileName);
            if (mixinCache.Load() && mixinCache.Nodes.Count > 0)
            {
                Application.Log(SeverityFlags.None, "Loaded {0} Mixin(s) from root", mixinCache.Nodes.Count - 1);
                foreach (StorageContainer mixin in mixinCache.Nodes.Values)
                    if(mixin.Tag == 0)
                        Application.Log(SeverityFlags.None, "  {0}", mixin.Element.GetRelativePath(Application.SdkDirectory));
            }
            mixinCache = null;

            Application.Log(SeverityFlags.None, "Loaded {0} Plugin(s) from root", plugins.Count);
            foreach (Plugin plugin in plugins)
                Application.Log(SeverityFlags.None, "  {0}", plugin.Path.GetRelativePath(Application.SdkDirectory));

            Application.Log(SeverityFlags.None, string.Empty);

            PageFormatter page = new PageFormatter();
            AutoConfig.GetAttributePage<Application>(page);
            page.Sort();

            PageFormatter subPage = new PageFormatter();
            AutoConfig.GetAttributePage(Application.Profile.GetType(), subPage);
            AutoConfig.GetAttributePage(Application.Profile, subPage);
            subPage.Sort();

            if (subPage.Rows.Count > 0)
            {
                page.AddRow("");
                page.AddRow(string.Format("{0} Profile", Application.Profile.Name.ToTitleCase()));
                page.Rows.AddRange(subPage.Rows);
            }

            Dictionary<string, PageFormatter> subSystems = new Dictionary<string, PageFormatter>();
            AppDomain.CurrentDomain.GetAssemblies().ParallelFor((assembly) =>
            {
                foreach (Type receiver in assembly.GetTypes<IConfigReceiver>())
                        try
                        {
                            PageFormatter systemPage = new PageFormatter();
                            AutoConfig.GetAttributePage(receiver, systemPage);
                            
                                IConfigReceiver instance; if (receiver.IsAbstract && MixinType.IsAssignableFrom(receiver))
                                    instance = MixinManager.Create(receiver) as IConfigReceiver;
                                else if (!receiver.IsAbstract)
                                    instance = receiver.CreateInstance<IConfigReceiver>();
                                else
                                    continue;

                                if (instance != null && instance.Display)
                                {
                                    instance.GetAdditionalManualInfo(systemPage);
                                    if (systemPage.Rows.Count > 0)
                                        lock (subSystems)
                                        {
                                            PageFormatter tmp; if (subSystems.TryGetValue(instance.DisplayName, out tmp))
                                                tmp.AddRows(systemPage.Rows);
                                            else
                                                subSystems.Add(instance.DisplayName, systemPage);
                                        }
                            }
                        }
                        catch
                        { }
            });
            foreach (KeyValuePair<string, PageFormatter> systems in subSystems.OrderBy(x => x.Key))
            {
                systems.Value.Sort();

                page.AddRow("");
                page.AddRow(string.Format("{0} System", systems.Key));
                page.Rows.AddRange(systems.Value.Rows);
            }
            foreach (Plugin plugin in plugins)
            {
                subPage = new PageFormatter();
                plugin.GetManualPage(subPage);
                subPage.Sort();

                if (subPage.Rows.Count > 0)
                {
                    page.AddRow("");
                    page.AddRow(string.Format("{0} Plugin", plugin.Path.Name.ToTitleCase()));
                    page.Rows.AddRange(subPage.Rows);
                }
            }

            Application.Log(SeverityFlags.None, page.ToString());
        }
    }
}
