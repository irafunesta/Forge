// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Threading;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Threading;
using SE.Parallel;
using SE.App;

namespace SE.Forge.Systems
{
    public partial class TaskGraph : IReceiver
    {
        const int TimerTickInterval = 200;
        private readonly static TaskGraph instance;

        private static QueuedChannel<Task> dispatcher;
        private static QueueBuffer<Task> pendingTasks;

        private static int activeTasks = 0;
        public static int Tasks
        {
            get { return Interlocked.CompareExchange(ref activeTasks, 0, 0); }
        }
        
        private static int errors = 0;
        public static int Errors
        {
            get { return Interlocked.CompareExchange(ref errors, 0, 0); }
        }

        static TaskGraph()
        {
            instance = new TaskGraph();
            dispatcher = new QueuedChannel<Task>();
            pendingTasks = new QueueBuffer<Task>(64);
            comparer = new TaskPinComparer();
        }
        public TaskGraph()
        { }

        public static void SetEndPoints(IEnumerable<KeyValuePair<Adapter, Action<Task>>> endPoints)
        {
            dispatcher.Clear();
            foreach (KeyValuePair<Adapter, Action<Task>> adapter in endPoints)
                dispatcher.Register(adapter.Key, adapter.Value);
        }

        public static void Dispatch(Task task)
        {
            if (task == null || !task.Dispatch())
                return;

            Interlocked.Increment(ref activeTasks);
            if (!dispatcher.Dispatch(instance, task))
                throw new EntryPointNotFoundException();
        }
        public static void Dispatch(IEnumerable<Task> tasks)
        {
            foreach (Task token in tasks)
                Dispatch(token);
        }

        public static bool AwaitCompletion(IEnumerable<Task> initialTasks)
        {
            Dispatch(initialTasks);
            while (Interlocked.CompareExchange(ref activeTasks, 0, 0) > 0)
            {
                if (!Application.ProcessLogMessages())
                    Thread.Sleep(TimerTickInterval);

                Task task; while (pendingTasks.Dequeue(out task))
                {
                    if (!task.IsPending)
                    {
                        if (!dispatcher.Dispatch(instance, task))
                            throw new EntryPointNotFoundException();
                    }
                    else if (!pendingTasks.Enqueue(task))
                        throw new IndexOutOfRangeException();
                }
            }
            return (Interlocked.CompareExchange(ref errors, 0, 0) == 0);
        }

        public static void AddSibling(Task root, Task sibling)
        {
            if (root != null)
            {
                if (root.Next == null) root.Next = sibling;
                else
                {
                    Task prevNext = root.Next;
                    for (; ; )
                    {
                        if (prevNext.Next != null) prevNext = prevNext.Next;
                        else
                        {
                            prevNext.Next = sibling;
                            break;
                        }
                    }

                }
            }
        }
        public static void AddChild(Task root, Task child)
        {
            if (root.Child == null) root.Child = child;
            else
            {
                Task prevChild = root.Child;
                for (; ; )
                {
                    if (prevChild.Next != null) prevChild = prevChild.Next;
                    else
                    {
                        prevChild.Next = child;
                        break;
                    }
                }
            }
        }

        public void SetResult(object host, object result)
        {
            AdapterContextBase context = (host as AdapterContextBase);
            if (context != null)
            {
                Task task = (context.Args[0] as Task);
                if (task.IsPending)
                {
                    while (!pendingTasks.Enqueue(task))
                        ;
                }
                else
                {
                    if (task.Child == null && task.OutputPins.Length > 0)
                        GenerateInputTasks(new List<Task>(new Task[]{ task }));

                    Task child = task.Child;
                    while (child != null)
                    {
                        Dispatch(child);
                        child = child.Next;
                    }
                    Interlocked.Decrement(ref activeTasks);
                }
            }
        }
        public void SetError(object host, Exception error)
        {
            AdapterContextBase context = (host as AdapterContextBase);
            if (context != null)
            {
                if (Application.LogSeverity > SeverityFlags.Minimal) Application.Error(SeverityFlags.None, "{0}{1}{2}", error.Message, Environment.NewLine, error.StackTrace);
                else Application.Error(SeverityFlags.None, error.Message);

                Interlocked.Increment(ref errors);
                Interlocked.Decrement(ref activeTasks);
            }
        }
    }
}
