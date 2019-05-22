// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Code.Analytics;
using SE.Threading;
using SE.Parallel;
using SE.Config;

namespace SE.Forge.Systems.Profiles
{
    public partial class LocalProfile : IBuildProfile
    {
        public const string ProfileName = "Local";

        public string Name
        {
            get { return ProfileName; }
        }

        public LocalProfile()
        {
            AutoConfigResult result = AutoConfigResult.Create();
            AutoConfig.MapToObject<LocalProfile>(true, ref result);
            if (printToolStack)
            {
                toolStackGraph = new ReferenceGraph();
                toolGraphLock = new PooledSpinLock();
            }
        }
        public void Dispose()
        {
            SaveTrackingResults();
        }
        
        public KeyValuePair<Adapter, Action<Task>>[] GetEndPoints()
        {
            return new KeyValuePair<Parallel.Adapter, Action<Task>>[]
                {
                    new KeyValuePair<Parallel.Adapter, Action<Task>>
                    (
                        new Parallel.Adapter(new Parallel.PoolingBehavior()),
                        LocalProfile.ProcessLocalTask
                    )
                };
        }

        internal static void ProcessLocalTask(Task task)
        {
            if (task.IsPending) return;
            else
            {
                task.Execute();
                if (printToolStack)
                    TrackTask(task);
            }
        }
    }
}
