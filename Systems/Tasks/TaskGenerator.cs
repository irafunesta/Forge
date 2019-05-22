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
        private const int PermutationLimit = 6;

        private static TaskPinComparer comparer;

        void GenerateInputTasks(List<Task> parentTasks)
        {
            List<Task> tasks = new List<Task>();
            if (parentTasks.Count < PermutationLimit)
            {
                ICollection<ICollection<Task>> tuple = parentTasks.ParallelGetPermutations();
                tuple.ParallelFor((set) =>
                {
                    TaskPin[] outPins = set.SelectMany(x => x.OutputPins).ToArray();
                    if (outPins.Length <= 1)
                        return;

                    foreach (ITaskPrototype prototype in TaskPrototypes.Get(outPins.Length))
                        if (prototype.InputPins.IsEqual(outPins, comparer))
                        {
                            Task result = prototype.CreateInstance();

                            Dictionary<TaskPin, TaskPin> inPins = result.InputPins.ToDictionary<TaskPin, TaskPin>(x => x, comparer);
                            foreach (TaskPin outPin in outPins)
                            {
                                inPins[outPin].Parent = outPin;
                                inPins.Remove(outPin);
                            }
                            lock (tasks)
                            {
                                foreach (Task parent in set)
                                    AddChild(parent, result);

                                tasks.Add(result);
                            }
                        }
                });
            }
            {
                TaskPin[] outPins = parentTasks.SelectMany(x => x.OutputPins).ToArray();
                TaskPrototypes.Get(0).ParallelFor((prototype) =>
                {
                    List<TaskPin> pins = new List<TaskPin>();
                    foreach (TaskPin outPin in outPins)
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
                    foreach (ITaskPrototype prototype in TaskPrototypes.Get(1))
                        if (prototype.InputPins[0].Accepts(outPin))
                        {
                            Task result = prototype.CreateInstance();
                            result.InputPins[0].Parent = outPin;

                            AddChild(task, result);
                            lock(tasks)
                                tasks.Add(result);
                        }
            });

            if (tasks.Count > 0)
                GenerateInputTasks(tasks);
        }
    }
}
