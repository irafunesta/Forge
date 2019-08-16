// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Reflection;
using SE.Threading;

namespace SE.Forge.Systems.Tasks
{
    public static class TaskPrototypes
    {
        private static PooledSpinLock tasksLock = new PooledSpinLock();
        private static Dictionary<UInt64, List<ITaskPrototype>> prototypes = new Dictionary<UInt64, List<ITaskPrototype>>();
        
        public static void Add(ITaskPrototype prototype)
        {
            if (!prototype.Enabled)
                return;

            UInt64 priority = (UInt64)TaskPriorityFlag.Primary;
                TaskPriorityAttribute attrib; if (prototype.GetType().TryGetAttribute<TaskPriorityAttribute>(out attrib))
                    priority = (UInt64)attrib.Priority;

            UInt64 id = (((UInt64)prototype.InputPins.Length << 32) | priority);
            using (new Scope(tasksLock))
            {
                List<ITaskPrototype> tmp; if (!prototypes.TryGetValue(id, out tmp))
                {
                    tmp = new List<ITaskPrototype>();
                    prototypes.Add(id, tmp);
                }
                tmp.Add(prototype);
            }
        }
        public static void AddRange(IEnumerable<ITaskPrototype> prototypes)
        {
            foreach (ITaskPrototype prototype in prototypes)
                Add(prototype);
        }

        public static IEnumerable<ITaskPrototype> Get(int count, TaskPriorityFlag priority)
        {
            UInt64 id = (((UInt64)count << 32) | (UInt64)priority);
            using (new Scope(tasksLock))
            {
                List<ITaskPrototype> tmp; if (!prototypes.TryGetValue(id, out tmp))
                {
                    tmp = new List<ITaskPrototype>();
                    prototypes.Add(id, tmp);
                }
                return tmp.ToArray();
            }
        }

        public static void Remove(ITaskPrototype prototype)
        {
            UInt64 priority = (UInt64)TaskPriorityFlag.Primary;
            TaskPriorityAttribute attrib; if (prototype.GetType().TryGetAttribute<TaskPriorityAttribute>(out attrib))
                priority = (UInt64)attrib.Priority;

            UInt64 id = (((UInt64)prototype.InputPins.Length << 32) | priority);
            using (new Scope(tasksLock))
            {
                List<ITaskPrototype> tmp; if (!prototypes.TryGetValue(id, out tmp))
                    return;

                tmp.Remove(prototype);
            }
        }
    }
}
