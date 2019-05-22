// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Threading;

namespace SE.Forge.Systems.Tasks
{
    public abstract class VariadicTask : Task, ITaskPrototype
    {
        public virtual bool Enabled
        {
            get { return true; }
        }

        public override bool IsPending
        {
            get 
            {
                lock(inputPins)
                {
                    for (int i = 0; i < inputPins.Count; i++)
                    {
                        if (inputPins[i].Parent != null && inputPins[i].Parent.Disposed)
                        {
                            inputPins.RemoveAt(i);
                            i--;
                        }
                        else if (inputPins[i].Parent == null || !inputPins[i].Parent.HasData)
                            return true;
                    }
                }
                return false;
            }
        }

        protected List<TaskPin> inputPins;
        public override TaskPin[] InputPins
        {
            get { return inputPins.ToArray(); }
        }

        public VariadicTask()
        {
            inputPins = new List<TaskPin>();
        }

        public void Add(TaskPin inputPin)
        {
            TaskPin pin = new TaskPin(this, inputPin.DataType);
            pin.Parent = inputPin;

            lock(inputPins)
                inputPins.Add(pin);
        }
        public void AddRange(IEnumerable<TaskPin> inputPins)
        {
            foreach(TaskPin inputPin in inputPins)
                Add(inputPin);
        }

        public abstract bool VariadicAccepts(TaskPin inputPin);
        public abstract Task CreateInstance();
    }
}
