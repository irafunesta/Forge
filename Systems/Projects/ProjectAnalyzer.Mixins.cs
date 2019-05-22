// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Threading;
using SE.Mixins;

namespace SE.Forge.Systems
{
    public abstract partial class ProjectAnalyzer : Mixin
    {
        private static SpinLock initializerLock = new SpinLock();

        private static ProjectAnalyzer instance;
        private static ProjectAnalyzer Instance
        {
            get
            {
                if (instance == null)
                    using (new Scope(initializerLock))
                    {
                        if (instance == null)
                            instance = MixinManager.Create<ProjectAnalyzer>();
                    }
                return instance;
            }
        }

        public ProjectAnalyzer()
        { }

        /**
         AddAdditionalProjectFiles
        */
        public abstract void AddAdditionalProjectFiles(Project project);
        [Mixin(typeof(ProjectAnalyzer), Order = MixinOrder.Default)]
        public static void AddAdditionalProjectFiles(object @this, Project project)
        { }
        public static void AddProjectFiles(Project project)
        {
            Instance.AddAdditionalProjectFiles(project);
        }

        /**
         AddAdditionalProjectFiles
        */
        public abstract void GetProjectAssemblyName(Project project, ref string name);
        [Mixin(typeof(ProjectAnalyzer), Order = MixinOrder.Default)]
        public static void GetProjectAssemblyName(object @this, Project project, ref string name)
        {
            name = project.Name;
        }
        public static string GetAssemblyName(Project project)
        {
            string alias = string.Empty;
            Instance.GetProjectAssemblyName(project, ref alias);

            return alias;
        }
    }
}
