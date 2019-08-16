// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.Systems
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class TaskPriorityAttribute : Attribute
    {
        TaskPriorityFlag flag;
        public TaskPriorityFlag Flag
        {
            get { return flag; }
        }

        public int Priority
        {
            get { return (int)flag; }
        }

        public TaskPriorityAttribute(TaskPriorityFlag flag)
        {
            this.flag = flag;
        }
    }
}
