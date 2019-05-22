// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Code.Build;
using SE.Reflection;
using SE.Threading;
using SE.Parallel;
using SE.Mixins;
using SE.Config;

namespace SE.Forge.CppCompiler
{
    public static class CompilerServices
    {
        private readonly static Type MixinType = typeof(Mixin);
        private static PooledSpinLock serviceLock = new PooledSpinLock();
        private static List<ICppCompilerService> services;
        private static BuildTarget[] targets;

        private static void LoadServices()
        {
            if (services == null)
            {
                services = new List<ICppCompilerService>();
                AppDomain.CurrentDomain.GetAssemblies().ParallelFor((assembly) =>
                {
                    foreach (Type service in assembly.GetTypes<ICppCompilerService>())
                        try
                        {
                            ICppCompilerService instance; if (service.IsAbstract && MixinType.IsAssignableFrom(service))
                                instance = MixinManager.Create(service) as ICppCompilerService;
                            else if (!service.IsAbstract)
                                instance = service.CreateInstance<ICppCompilerService>();
                            else
                                continue;

                            if (instance != null)
                                services.Add(instance);
                        }
                        catch
                        { }
                });
            }
        }

        public static IEnumerable<ICppCompilerService> Services
        {
            get
            {
                if (services == null)
                    using (new Scope(serviceLock))
                        if (services == null)
                        {
                            LoadServices();
                        }

                return services;
            }
        }
        public static BuildTarget[] Targets 
        {
            get
            {
                if (targets == null)
                    using (new Scope(serviceLock))
                        if (targets == null)
                        {
                            LoadServices();
                            Dictionary<UInt32, BuildTarget> tmp = new Dictionary<UInt32, BuildTarget>();
                            services.ParallelFor((service) =>
                            {
                                foreach (TargetArchitecture architecture in service.Architectures)
                                    foreach (TargetPlatform platform in service.Platforms)
                                    {
                                        List<string> values; if (CommandLineOptions.Options.TryGetValue(platform.GetTechnicalName(architecture).ToLowerInvariant(), out values))
                                            foreach (BuildMode mode in service.GetBuildModes(values))
                                            {
                                                BuildTarget target = new BuildTarget(platform, architecture, mode);
                                                lock (tmp)
                                                {
                                                    BuildTarget match; if (!tmp.TryGetValue(target.Id, out match))
                                                    {
                                                        target.Platform.SetDependentDefines(architecture);
                                                        tmp.Add(target.Id, target);
                                                    }
                                                    else
                                                        foreach (string key in target.Mode.Definitions.Keys)
                                                            if (!match.Mode.Definitions.ContainsKey(key))
                                                                match.Mode.Definitions.Add(key, target.Mode.Definitions[key]);
                                                }
                                            }
                                    }
                            });
                            targets = tmp.Values.ToArray();
                        }

                return targets;
            }
        }
    }
}
