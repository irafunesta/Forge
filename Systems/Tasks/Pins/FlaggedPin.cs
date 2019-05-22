// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.Systems.Pins
{
    public class FlaggedPin : TaskPin
    {
        protected UInt32 flags;
        public UInt32 Flags
        {
            get { return flags; }
        }

        public FlaggedPin(Task owner, Type dataType, object data, UInt32 flags)
            :base(owner, dataType, data)
        {
            this.flags = flags;
        }
        public FlaggedPin(Task owner, Type dataType, UInt32 flags)
            :this(owner, dataType, null, flags)
        { }
        public FlaggedPin(Task owner, object data, UInt32 flags)
            : this(owner, data.GetType(), data, flags)
        { }
    }
}
