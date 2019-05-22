// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Threading;

namespace SE.Forge.Systems.Tasks
{
    public abstract class MulticastTask : Task, ITaskPrototype
    {
        public virtual bool Enabled
        {
            get { return true; }
        }
        public override bool IsPending
        {
            get
            {
                for (int i = 0; i < InputPins.Length; i++)
                    if (InputPins[i].Parent == null || !InputPins[i].Parent.HasData)
                        return true;

                return false;
            }
        }

        public MulticastTask()
        { }

        public abstract bool VariadicAccepts(TaskPin inputPin);
        public abstract Task CreateInstance();
    }
}
