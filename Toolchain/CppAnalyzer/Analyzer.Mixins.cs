// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems;
using SE.Code.Build;
using SE.Storage;
using SE.Mixins;

namespace SE.Forge.CppAnalyzer
{
    public abstract partial class Analyzer : Mixin
    {
        private static Analyzer instance;
        private static Analyzer Instance
        {
            get
            {
                if (instance == null)
                    instance = MixinManager.Create<Analyzer>();

                return instance;
            }
        }

        public Analyzer()
        { }

        /**
         GetCppModulePaths
        */
        public abstract void GetCppModulePaths(List<PathDescriptor> modules);
        [Mixin(typeof(Analyzer), Order = MixinOrder.Default)]
        public static void GetCppModulePaths(object @this, List<PathDescriptor> modules)
        { }
        public static List<PathDescriptor> GetModulePaths()
        {
            List<PathDescriptor> modules = new List<PathDescriptor>();
            Instance.GetCppModulePaths(modules);

            return modules;
        }

        /**
         SetCppProjectType
        */
        public abstract void SetCppProjectType(Project project);
        [Mixin(typeof(Analyzer), Order = MixinOrder.Default)]
        public static void SetCppProjectType(object @this, Project project)
        { }
        public static void SetProjectType(Project project)
        {
            Instance.SetCppProjectType(project);
        }

        /**
         GetCppProjectConfigurations
        */
        public abstract void GetCppProjectConfigurations(List<BuildMode> configurations);
        [Mixin(typeof(Analyzer), Order = MixinOrder.Default)]
        public static void GetCppProjectConfigurations(object @this, List<BuildMode> configurations)
        {
            BuildMode mode = new BuildMode("Debug", true, false, false);
            mode.Definitions.Add("DEBUG", "1");
            configurations.Add(mode);

            mode = new BuildMode("Release", true, true, true);
            mode.Definitions.Add("NDEBUG", "1");
            configurations.Add(mode);
        }
        public static List<BuildMode> GetProjectConfigurations()
        {
            List<BuildMode> configurations = new List<BuildMode>();
            Instance.GetCppProjectConfigurations(configurations);

            return configurations;
        }

        /**
         SetCppProjectDependencies
        */
        public abstract void SetCppProjectDependencies(Project project);
        [Mixin(typeof(Analyzer), Order = MixinOrder.Default)]
        public static void SetCppProjectDependencies(object @this, Project project)
        { }
        public static void SetProjectDependencies(Project project)
        {
            Instance.SetCppProjectDependencies(project);
        }
    }
}
