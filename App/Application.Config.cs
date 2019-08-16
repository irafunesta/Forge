// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SE.Forge.Systems.Profiles;
using SE.Forge.Systems;
using SE.Forge.Config;
using SE.Reflection;
using SE.Threading;
using SE.Storage;
using SE.Mixins;
using SE.Config;
using SE.Text;

namespace SE.App
{
    public abstract partial class Application
    {
        public const string BuildDirectoryName = ".build";
        public const string DeployDirectoryName = "Deploy";

        public const string CacheCleanupParameter = "CacheCleanup";
        public const string SdkDirectoryParameter = "SdkRoot";
        public const string ProfileParameter = "Profile";
        public const string ProjectParameter = "Project";
        public const string BuildParameter = "Build";
        

        public const string AllowLocalFallbackParameter = "AllowLocalFallback";
        public const string BuildSystemsFilterParameter = "BuildSystems";
        public const string DebugInfoParameter = "DebugInfo";
        public const string AutoToolsetParameter = "AutoToolset";
        public const string RebuildParameter = "Rebuild";

        enum ApplicationFlags : byte
        {
            None = 0,
            Manual = 0x1,
            ClearCache = 0x2,
        }

        [AutoConfig('?', Text = "Shows this manual page", FlagIndex = 0)]
        [AutoConfig(CacheCleanupParameter, Text = "Starts without any cache files", FlagIndex = 1)]
        private static ApplicationFlags flags = ApplicationFlags.None;

        public static bool DisplayManual
        {
            get { return ((flags & ApplicationFlags.Manual) == ApplicationFlags.Manual); }
        }
        public static bool ClearCache
        {
            get { return ((flags & ApplicationFlags.ClearCache) == ApplicationFlags.ClearCache); }
        }

        [AutoConfig(BuildParameter, Text = "Defines a path to project directories and/or files", TypeConverter = typeof(StorageConverter), DefaultIndex = 0)]
        private static List<FileSystemDescriptor> buildEntries = new List<FileSystemDescriptor>();
        [AutoConfig(ProjectParameter, Text = "Defines path to a single project directory", TypeConverter = typeof(StorageConverter))]
        private static List<PathDescriptor> projectPaths = new List<PathDescriptor>();
        [AutoConfig(SdkDirectoryParameter, Text = "Defines path to the SDK root folder", TypeConverter = typeof(StorageConverter))]
        private static PathDescriptor sdkDirectory = null;
        private static PathDescriptor buildDirectory;
        private static PathDescriptor cacheDirectory;
        private static PathDescriptor configDirectory;
        private static PathDescriptor deployDirectory;

        public static PathDescriptor SdkDirectory
        {
            get 
            {
                if (sdkDirectory == null)
                {
                    foreach (FileSystemDescriptor entry in buildEntries)
                    {
                        string result = GetTopLevelPath(entry.GetAbsolutePath());
                        if (result != RootPath)
                        {
                            sdkDirectory = new PathDescriptor(result);
                            break;
                        }
                    }
                    if (sdkDirectory == null)
                        sdkDirectory = new PathDescriptor(GetTopLevelPath(null));
                }
                return sdkDirectory;
            }
        }
        public static PathDescriptor BuildDirectory
        {
            get
            {
                if (buildDirectory == null)
                    buildDirectory = new PathDescriptor(SdkDirectory, BuildDirectoryName);

                return buildDirectory;
            }
        }
        public static PathDescriptor CacheDirectory
        {
            get 
            {
                if (cacheDirectory == null)
                {
                    cacheDirectory = new PathDescriptor(SdkDirectory, CacheDirectoryName);
                    if (!cacheDirectory.Exists())
                        cacheDirectory.CreateHidden();
                    cacheDirectory = new PathDescriptor(cacheDirectory, Name);
                }
                return cacheDirectory;
            }
        }
        public static PathDescriptor ConfigDirectory
        {
            get
            {
                if (configDirectory == null)
                    configDirectory = new PathDescriptor(new PathDescriptor(SdkDirectory, ConfigDirectoryName), Name);

                return configDirectory;
            }
        }
        public static PathDescriptor DeployDirectory
        {
            get
            {
                if (deployDirectory == null)
                    deployDirectory = new PathDescriptor(SdkDirectory, DeployDirectoryName);

                return deployDirectory;
            }
        }

        public static List<PathDescriptor> GetBuildPaths()
        {
            List<PathDescriptor> paths = new List<PathDescriptor>();
            foreach (PathDescriptor path in buildEntries.Where(x => (x is PathDescriptor)))
            {
                ResolveBuildPaths(projectPaths, path);
                paths.Add(path);
            }
            foreach (PathDescriptor path in paths)
                buildEntries.Remove(path);
            foreach (PathDescriptor path in projectPaths)
                path.Equalize();

            return projectPaths;
        }
        public static IEnumerable<FileDescriptor> GetBuildFiles()
        {
            return buildEntries.Where(x => (x is FileDescriptor)).Cast<FileDescriptor>();
        }

        [AutoConfig(AllowLocalFallbackParameter, Text = "Sets profile to local if anything else fails")]
        private static bool allowLocalFallback = true;

        [AutoConfig(ProfileParameter, Text = "Sets build profile at least to one of [Local|Remote|Cloud]")]
        private static UInt32 buildProfileId = 0;
        private static IBuildProfile profile;
        public static IBuildProfile Profile
        {
            get
            {
                using (new Scope(buildProfileLock))
                {
                    if (profile == null)
                    {
                        Type profileType; if (!buildProfiles.TryGetValue(buildProfileId, out profileType))
                        {
                            if (allowLocalFallback) profileType = typeof(LocalProfile);
                            else throw new InvalidCastException(string.Format("'{0}' is not a valid profile", buildProfileId));
                        }

                        if (typeof(Mixin).IsAssignableFrom(profileType))
                            profile = (IBuildProfile)MixinManager.Create(profileType);
                        else
                            profile = profileType.CreateInstance<IBuildProfile>();
                    }
                    return profile;
                }
            }
        }

        [AutoConfig(BuildSystemsFilterParameter, Text = "Defines a list of build systems to use on file detection")]
        private static List<UInt32> buildSystemFilter = new List<UInt32>();

        [AutoConfig(DebugInfoParameter, Text = "Sets the state of debug info output")]
        private static bool generateDebugInfo = false;
        public static bool GenerateDebugInfo
        {
            get { return generateDebugInfo; }
        }

        [AutoConfig(RebuildParameter, Text = "Invalidates cached projects and forces a rebuild")]
        private static bool rebuild = false;
        public static bool Rebuild
        {
            get { return rebuild; }
        }

        /*[AutoConfig(AutoToolsetParameter, Text = "Sets toolset generator to default project files output")]
        private static bool autoToolset = false;
        public static bool AutoToolset
        {
            get { return autoToolset; }
        }*/
    }
}
