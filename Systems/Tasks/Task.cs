// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Threading;

namespace SE.Forge.Systems
{
    [TaskPriority(TaskPriorityFlag.Primary)]
    public abstract class Task : IDisposable
    {
        int dispatchedFlag;
        public bool Dispatched
        {
            get { return (Interlocked.CompareExchange(ref dispatchedFlag, 0, 0) > 0); }
        }

        protected Task child;
        public Task Child
        {
            get { return child; }
            set { child = value; }
        }

        protected Task next;
        public Task Next
        {
            get { return next; }
            set { next = value; }
        }

        public abstract TaskPin[] InputPins
        {
            get;
        }
        public abstract TaskPin[] OutputPins
        {
            get;
        }

        public virtual bool IsPending
        {
            get { return false; }
        }

        public Task()
        { }
        public virtual bool Dispatch()
        {
            return (Interlocked.CompareExchange(ref dispatchedFlag, 1, 0) == 0);
        }

        public abstract void Execute();
        protected void LoadInput()
        {
            foreach (TaskPin inputPin in InputPins)
                if (inputPin.Parent != null)
                    inputPin.Data = inputPin.Parent.Data;
        }

        public virtual void Dispose()
        {
            foreach (TaskPin pin in InputPins)
                pin.Dispose();
            foreach (TaskPin pin in OutputPins)
                pin.Dispose();
        }
    }
}
