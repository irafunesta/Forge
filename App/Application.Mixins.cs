// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Storage;
using SE.Mixins;
using SE.Text;

namespace SE.App
{
    public abstract partial class Application : Mixin
    {
        private static Application instance;
        private static Application Instance
        {
            get
            {
                if (instance == null)
                    instance = MixinManager.Create<Application>();

                return instance;
            }
        }

        public Application()
        { }

        /**
         GetBuildPath
        */
        public abstract PathDescriptor GetBuildPathFromTag(UInt32 tag);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static PathDescriptor GetBuildPathFromTag(object @this, UInt32 tag)
        {
            return BuildDirectory;
        }
        public abstract PathDescriptor GetBuildPathFromPath(PathDescriptor path);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static PathDescriptor GetBuildPathFromPath(object @this, PathDescriptor path)
        {
            return BuildDirectory;
        }

        public static PathDescriptor GetBuildPath(UInt32 tag)
        {
            return Instance.GetBuildPathFromTag(tag);
        }
        public static PathDescriptor GetBuildPath(PathDescriptor path)
        {
            return Instance.GetBuildPathFromPath(path);
        }

        /**
         GetDeploymentPath
        */
        public abstract PathDescriptor GetDeploymentPathFromTag(UInt32 tag);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static PathDescriptor GetDeploymentPathFromTag(object @this, UInt32 tag)
        {
            return DeployDirectory;
        }
        public abstract PathDescriptor GetDeploymentPathFromPath(PathDescriptor path);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static PathDescriptor GetDeploymentPathFromPath(object @this, PathDescriptor path)
        {
            return DeployDirectory;
        }

        public static PathDescriptor GetDeploymentPath(UInt32 tag)
        {
            return Instance.GetDeploymentPathFromTag(tag);
        }
        public static PathDescriptor GetDeploymentPath(PathDescriptor path)
        {
            return Instance.GetDeploymentPathFromPath(path);
        }

        /**
         GetPluginList
        */
        public abstract void GetPluginList(List<PathDescriptor> plugins);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static void GetPluginList(object @this, List<PathDescriptor> plugins)
        { }
        public static List<PathDescriptor> GetPluginList()
        {
            List<PathDescriptor> plugins = new List<PathDescriptor>();
            Instance.GetPluginList(plugins);

            return plugins;
        }

        /**
         ExcludeFromBuild
        */
        public abstract void ExcludeFromBuild(List<PathDescriptor> projects);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static void ExcludeFromBuild(object @this, List<PathDescriptor> projects)
        { }

        /**
         GetPluginFilesFilter
        */
        public abstract void GetPluginFilesFilter(Filter filter);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static void GetPluginFilesFilter(object @this, Filter filter)
        {
            filter.Clear();
            FilterToken token = filter.Add("*.Build.cs");
            token.Type = FilterType.And;
            token.Exclude = true;
            token = filter.Add("*.cs");
        }
        public static List<FileSystemDescriptor> GetPluginFiles(PathDescriptor path)
        {
            Filter filter = new Filter();

            Instance.GetPluginFilesFilter(filter);
            return path.FindFiles(filter);
        }

        /**
         GetBuildPathFilter
        */
        public abstract void GetBuildPathFilter(Filter filter);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static void GetBuildPathFilter(object @this, Filter filter)
        {
            filter.Clear();
            FilterToken token = filter.Add("...");
            token = filter.Add(token, ".*");
            token.Exclude = true;
        }
        public static void ResolveBuildPaths(List<PathDescriptor> paths, PathDescriptor buildRoot)
        {
            Filter filter = new Filter();

            Instance.GetBuildPathFilter(filter);
            paths.AddRange(buildRoot.FindDirectories(filter).Cast<PathDescriptor>());
            Instance.ExcludeFromBuild(paths);
        }

        /**
         GetExcludedFiles
        */
        public abstract void GetForgeExcludedFiles(List<string> excludes);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static void GetForgeExcludedFiles(object @this, List<string> excludes)
        {
            excludes.Add("*.Build.cs");
            excludes.Add("*.Setup.cs");
        }
        public static void LoadFileExclusion()
        {
            List<string> excludes = new List<string>();
            Instance.GetForgeExcludedFiles(excludes);
            exclusions = excludes.ToArray();
        }

        /**
         GetProjectFileFilter
        */
        private static string[] exclusions;

        public abstract void GetProjectFileFilter(Filter filter, string[] extensions);
        [Mixin(typeof(Application), Order = MixinOrder.Default)]
        public static void GetProjectFileFilter(object @this, Filter filter, string[] extensions)
        {
            filter.Clear();
            foreach (string extension in extensions)
            {
                if (extension == "cs")
                {
                    if (exclusions == null)
                        lock (Instance)
                            if (exclusions == null)
                                LoadFileExclusion();

                    foreach (string exclusion in exclusions)
                    {
                        FilterToken token = filter.Add(exclusion);
                        token.Type = FilterType.And;
                        token.Exclude = true;
                    }
                }
                filter.Add(string.Format("*.{0}", extension));
            }
        }
        public static List<FileSystemDescriptor> GetProjectFiles(PathDescriptor projectPath, string[] extensions)
        {
            Filter filter = new Filter();
            Instance.GetProjectFileFilter(filter, extensions);
            return projectPath.FindFiles(filter);
        }
    }
}
