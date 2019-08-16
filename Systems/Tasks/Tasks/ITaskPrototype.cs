// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.Systems.Tasks
{
    public interface ITaskPrototype
    {
        bool Enabled { get; }
        TaskPin[] InputPins { get; }
        TaskPin[] OutputPins { get; }

        bool ExclusiveUse { get; }

        bool VariadicAccepts(TaskPin inputPin);
        Task CreateInstance();
    }
}
