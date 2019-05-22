// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SE.Forge.Systems;
using SE.Threading;
using SE.Parallel;
using SE.Storage;
using SE.Text;

namespace SE.App
{
    public abstract partial class Application
    {
        private static QueueBuffer<KeyValuePair<byte, string>> logBuffer = new QueueBuffer<KeyValuePair<byte, string>>(64);
        private static int mainScopeId;

        private static PooledSpinLock buildProfileLock = new PooledSpinLock();
        private static Dictionary<UInt32, Type> buildProfiles = new Dictionary<UInt32, Type>();
        public static IEnumerable<Type> Profiles
        {
            get { return buildProfiles.Values; }
        }

        private static ILogSystem logSystem = new ConsoleLogSystem();
        public static ILogSystem LogSystem
        {
            get { return logSystem; }
            set { logSystem = value; }
        }

        //Routed .ctor call
        static partial void OnLoad()
        {
            mainScopeId = Scope.LocalId;
        }
        //Routed LoadManifest([]args) call
        static partial void OnConfigRequested()
        {
            manifestFile = new FileDescriptor(ConfigDirectory, "Settings.json").GetAbsolutePath();
        }
        //Routed LoadManifest([]args)/Initialize([]args) call
        static partial void OnInitialize()
        {
            foreach (FileSystemDescriptor entry in buildEntries)
                if(entry.Exists())
                    entry.Equalize();
        }

        public static bool RegsiterProfile(string name, Type profileType, bool @override)
        {
            UInt32 id = name.ToLowerInvariant().Fnv32();
            using (new Scope(buildProfileLock))
            {
                if (buildProfiles.ContainsKey(id))
                {
                    if (@override) buildProfiles[id] = profileType;
                    else return false;
                }
                else buildProfiles.Add(id, profileType);
            }
            return true;
        }
        public static bool RegsiterProfile(string name, Type profileType)
        {
            return RegsiterProfile(name, profileType, false);
        }

        static partial void LogInternal(string message, params object[] args)
        {
            if (Scope.LocalId == mainScopeId) logSystem.Log(message, args);
            else while (!logBuffer.Enqueue(new KeyValuePair<byte, string>(2, string.Format(message, args)))) ;
        }
        static partial void LogWarningInternal(string message, params object[] args)
        {
            if (Scope.LocalId == mainScopeId) logSystem.Warning(message, args);
            else while (!logBuffer.Enqueue(new KeyValuePair<byte, string>(1, string.Format(message, args)))) ;
        }
        static partial void LogErrorInternal(string message, params object[] args)
        {
            if (Scope.LocalId == mainScopeId) logSystem.Error(message, args);
            else while (!logBuffer.Enqueue(new KeyValuePair<byte, string>(0, string.Format(message, args)))) ;
        }

        public static bool ProcessLogMessages()
        {
            KeyValuePair<byte, string> message; while (logBuffer.Dequeue(out message))
            {
                switch (message.Key)
                {
                    case 0: logSystem.Error(message.Value); break;
                    case 1: logSystem.Warning(message.Value); break;
                    default: logSystem.Log(message.Value); break;
                }
            }
            return !string.IsNullOrWhiteSpace(message.Value);
        }
    }
}
