// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.Systems.Pins
{
    public class TaskPinComparer : IEqualityComparer<TaskPin>
    {
        public bool Equals(TaskPin x, TaskPin y)
        {
            return x.Accepts(y);
        }
        public int GetHashCode(TaskPin obj)
        {
            return obj.DataType.GetHashCode();
        }
    }
}
