// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Code;
using System.Text;

namespace SE.Forge.CppPreprocessor
{
    public class Macro
    {
        UInt32 id;
        public UInt32 Id
        {
            get { return id; }
        }

        List<UInt32> parameter;
        public List<UInt32> Parameter
        {
            get { return parameter; }
        }

        TokenStream replacementList;
        public TokenStream ReplacementList
        {
            get { return replacementList; }
        }

        bool variadic;
        public bool Variadic
        {
            get { return variadic; }
            set { variadic = value; }
        }

        public Macro(UInt32 id)
        {
            this.id = id;
            this.parameter = new List<UInt32>();
            this.replacementList = new TokenStream();
        }
        public Macro(UInt32 id, string definition)
            : this(id)
        {
            replacementList.Buffer.AddRange(Encoding.UTF8.GetBytes(definition));
        }
    }
}
