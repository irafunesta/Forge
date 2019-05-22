// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Threading;

namespace SE.Forge.Systems.Tasks
{
    public static class TaskPrototypes
    {
        private static PooledSpinLock tasksLock = new PooledSpinLock();
        private static Dictionary<int, List<ITaskPrototype>> prototypes = new Dictionary<int, List<ITaskPrototype>>();
        
        public static void Add(ITaskPrototype prototype)
        {
            if (!prototype.Enabled)
                return;

            using (new Scope(tasksLock))
            {
                List<ITaskPrototype> tmp; if (!prototypes.TryGetValue(prototype.InputPins.Length, out tmp))
                {
                    tmp = new List<ITaskPrototype>();
                    prototypes.Add(prototype.InputPins.Length, tmp);
                }
                tmp.Add(prototype);
            }
        }
        public static void AddRange(IEnumerable<ITaskPrototype> prototypes)
        {
            foreach (ITaskPrototype prototype in prototypes)
                Add(prototype);
        }

        public static IEnumerable<ITaskPrototype> Get(int count)
        {
            using (new Scope(tasksLock))
            {
                List<ITaskPrototype> tmp; if (!prototypes.TryGetValue(count, out tmp))
                {
                    tmp = new List<ITaskPrototype>();
                    prototypes.Add(count, tmp);
                }
                return tmp.ToArray();
            }
        }

        public static void Remove(ITaskPrototype prototype)
        {
            using (new Scope(tasksLock))
            {
                List<ITaskPrototype> tmp; if (!prototypes.TryGetValue(prototype.InputPins.Length, out tmp))
                    return;

                tmp.Remove(prototype);
            }
        }
    }
}
