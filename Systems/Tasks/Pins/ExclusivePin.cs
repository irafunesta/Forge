// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.Systems.Pins
{
    public class ExclusivePin : FlaggedPin
    {
        public ExclusivePin(Task owner, Type dataType, object data, UInt32 flags)
            :base(owner, dataType, data, flags)
        { }
        public ExclusivePin(Task owner, Type dataType, UInt32 flags)
            : this(owner, dataType, null, flags)
        { }
        public ExclusivePin(Task owner, object data, UInt32 flags)
            : this(owner, data.GetType(), data, flags)
        { }

        public override bool Accepts(TaskPin outputPin)
        {
            return (outputPin is FlaggedPin && ((outputPin as FlaggedPin).Flags & flags) == flags) && base.Accepts(outputPin);
        }
    }
}
