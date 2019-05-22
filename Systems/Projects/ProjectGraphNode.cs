// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Code.Analytics;

namespace SE.Forge.Systems
{
    public class ProjectGraphNode : ReferenceNode
    {
        public override string Text
        {
            get { return file.Name; }
        }
        public ProjectGraphNode()
        { }

        public override void FetchExports()
        { }
        public override void FetchImports()
        { }
    }
}
