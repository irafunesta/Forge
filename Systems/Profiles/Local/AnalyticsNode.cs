// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Code.Analytics;

namespace SE.Forge.Systems.Profiles
{
    public class AnalyticsNode : ReferenceNode
    {
        protected string text;
        public override string Text
        {
            get { return text; }
        }

        protected int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public AnalyticsNode()
        {}
        
        public void FillData(object @object)
        {
            id = @object.GetHashCode();
            text = @object.ToString();
        }

        public override void FetchExports()
        {
            
        }
        public override void FetchImports()
        {
            
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
