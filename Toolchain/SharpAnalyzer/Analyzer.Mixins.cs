// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Code.Build;
using SE.Storage;
using SE.Mixins;

namespace SE.Forge.SharpAnalyzer
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
         GetSharpModulePaths
        */
        public abstract void GetSharpModulePaths(List<PathDescriptor> modules);
        [Mixin(typeof(Analyzer), Order = MixinOrder.Default)]
        public static void GetSharpModulePaths(object @this, List<PathDescriptor> modules)
        { }
        public static List<PathDescriptor> GetModulePaths()
        {
            List<PathDescriptor> modules = new List<PathDescriptor>();
            Instance.GetSharpModulePaths(modules);

            return modules;
        }

        /**
         GetSharpProjectConfigurations
        */
        public abstract void GetSharpProjectConfigurations(List<BuildMode> configurations);
        [Mixin(typeof(Analyzer), Order = MixinOrder.Default)]
        public static void GetSharpProjectConfigurations(object @this, List<BuildMode> configurations)
        {
            BuildMode mode = new BuildMode("Debug", true, false, false);
            mode.Definitions.Add("DEBUG", "1");
            configurations.Add(mode);

            mode = new BuildMode("Release", true, true, true);
            configurations.Add(mode);
        }
        public static List<BuildMode> GetProjectConfigurations()
        {
            List<BuildMode> configurations = new List<BuildMode>();
            Instance.GetSharpProjectConfigurations(configurations);

            return configurations;
        }
    }
}
