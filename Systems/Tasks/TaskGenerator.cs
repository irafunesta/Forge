// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Parallel;

namespace SE.Forge.Systems
{
    public partial class TaskGraph
    {
        private static TaskPinComparer comparer;

        void GetTasks(IEnumerable<ITaskPrototype> prototypes, IEnumerable<TaskPin> pins, List<Task> tasks)
        {
            foreach (ITaskPrototype proto in prototypes)
                if (proto.InputPins.IsEqual(pins, comparer) && proto.OutputPins.IsEqual(pins, comparer))
                {
                    Task t = proto.CreateInstance();
                    if (tasks.Count > 0)
                    {
                        Task parent = tasks[tasks.Count - 1];
                        Dictionary<TaskPin, TaskPin> inPins = t.InputPins.ToDictionary<TaskPin, TaskPin>(x => x, comparer);
                        foreach (TaskPin outPin in parent.OutputPins)
                            inPins[outPin].Parent = outPin;

                        AddChild(parent, t);
                    }
                    tasks.Add(t);
                }
        }

        void GenerateInputTasks(List<Task> parentTasks)
        {
            List<Task> tasks = new List<Task>();
            {
                ICollection<ICollection<Task>> tuple = parentTasks.ParallelGetCombinations(parentTasks.Count, 2);
                tuple.Reverse().ParallelFor((set) =>
                {
                    TaskPin[] outPins = set.SelectMany(x => x.OutputPins).ToArray();
                    if (outPins.Length <= 1)
                        return;

                    foreach (ITaskPrototype prototype in TaskPrototypes.Get(outPins.Length, TaskPriorityFlag.Primary))
                        if (prototype.InputPins.IsEqual(outPins, comparer))
                        {
                            Task result = prototype.CreateInstance();

                            Dictionary<TaskPin, TaskPin> inPins = result.InputPins.ToDictionary<TaskPin, TaskPin>(x => x, comparer);
                            List<TaskPin> connectedPins = new List<TaskPin>(inPins.Count);
                            foreach (TaskPin outPin in outPins)
                            {
                                TaskPin inPin; if (inPins.TryGetValue(outPin, out inPin))
                                {
                                    lock (outPin)
                                    {
                                        if (prototype.ExclusiveUse)
                                            outPin.Locked = true;
                                    }
                                    connectedPins.Add(outPin);
                                }
                            }

                            List<Task> taskCache = new List<Task>(8);
                            GetTasks(TaskPrototypes.Get(outPins.Length, TaskPriorityFlag.Before), connectedPins, taskCache);

                            if (taskCache.Count > 0)
                            {
                                Task parent = tasks[tasks.Count - 1];
                                inPins = result.InputPins.ToDictionary<TaskPin, TaskPin>(x => x, comparer);
                                foreach (TaskPin outPin in parent.OutputPins)
                                    inPins[outPin].Parent = outPin;

                                AddChild(parent, result);
                            }

                            taskCache.Add(result);
                            inPins = taskCache[0].InputPins.ToDictionary<TaskPin, TaskPin>(x => x, comparer);
                            foreach (TaskPin outPin in connectedPins)
                                inPins[outPin].Parent = outPin;

                            GetTasks(TaskPrototypes.Get(outPins.Length, TaskPriorityFlag.After), result.OutputPins, taskCache);
                            lock (tasks)
                            {
                                foreach (Task parent in set)
                                    AddChild(parent, taskCache[0]);

                                tasks.Add(taskCache[taskCache.Count - 1]);
                            }
                        }
                });
            }
            {
                TaskPin[] outPins = parentTasks.SelectMany(x => x.OutputPins).ToArray();
                TaskPrototypes.Get(0, TaskPriorityFlag.Primary).ParallelFor((prototype) =>
                {
                    List<TaskPin> pins = new List<TaskPin>();
                    foreach (TaskPin outPin in outPins)
                        if (!outPin.Locked)
                            if (prototype.VariadicAccepts(outPin))
                                pins.Add(outPin);

                    if (pins.Count > 0)
                    {
                        IEnumerable<Task> parents = pins.Select(x => x.Owner).Distinct();
                        VariadicTask result = (prototype.CreateInstance() as VariadicTask);
                        result.AddRange(pins);
                        lock(tasks)
                        {
                            foreach (Task parent in parents)
                                AddChild(parent, result);

                            tasks.Add(result);
                        }
                    }
                });
            }
            parentTasks.ParallelFor((task) =>
            {
                foreach (TaskPin outPin in task.OutputPins)
                    if (!outPin.Locked)
                    {
                        foreach (ITaskPrototype prototype in TaskPrototypes.Get(1, TaskPriorityFlag.Primary))
                            if (prototype.InputPins[0].Accepts(outPin))
                            {
                                List<Task> taskCache = new List<Task>(8);
                                Task result = prototype.CreateInstance();

                                GetTasks(TaskPrototypes.Get(1, TaskPriorityFlag.Before), new TaskPin[] { outPin }, taskCache);

                                if (taskCache.Count > 0)
                                {
                                    Task parent = tasks[tasks.Count - 1];
                                    result.InputPins[0].Parent = parent.OutputPins[0];

                                    AddChild(parent, result);
                                }

                                taskCache.Add(result);
                                taskCache[0].InputPins[0].Parent = outPin;

                                GetTasks(TaskPrototypes.Get(1, TaskPriorityFlag.After), new TaskPin[] { taskCache[0].OutputPins[0] }, taskCache);

                                AddChild(task, taskCache[0]);
                                lock (tasks)
                                    tasks.Add(taskCache[taskCache.Count - 1]);
                            }
                    }
            });

            if (tasks.Count > 0)
                GenerateInputTasks(tasks);
        }
    }
}
