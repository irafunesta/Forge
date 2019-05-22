// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SE.Code.Analytics;
using SE.Storage;
using SE.Forge.Systems;

namespace SE.Forge.SharpAnalyzer
{
    public class SharpReferenceNode : ReferenceNode
    {
        private static readonly KeyValuePair<int, string> GlobalNamespace = new KeyValuePair<int, string>();

        public override string Text
        {
            get { return file.FullName; }
        }

        protected List<KeyValuePair<int, string>> namespaces = new List<KeyValuePair<int, string>>();
        public List<KeyValuePair<int, string>> Namespaces
        {
            get { return namespaces; }
        }

        protected List<KeyValuePair<int, string>> declarations = new List<KeyValuePair<int, string>>();
        public List<KeyValuePair<int, string>> Declarations
        {
            get { return declarations; }
        }

        protected List<KeyValuePair<int, string>> namespaceReferences = new List<KeyValuePair<int, string>>();
        public List<KeyValuePair<int, string>> NamespaceReferences
        {
            get { return namespaceReferences; }
        }

        protected List<KeyValuePair<int, string>> typeReferences = new List<KeyValuePair<int, string>>();
        public List<KeyValuePair<int, string>> TypeReferences
        {
            get { return typeReferences; }
        }

        public SharpReferenceNode()
            : base()
        { }
        public SharpReferenceNode(FileDescriptor file)
            : base(file)
        { }

        public override void FetchExports()
        {
            string content = file.GetText(Encoding.UTF8);
            content = CodeParser.Prepare(content);

            OutputAssemblyType nodeType = OutputAssemblyType.Library;
            content = CodeParser.GetNodeType(content, ref nodeType);
            if (nodeType != OutputAssemblyType.Library)
            {
                flags.Add((UInt16)nodeType);
            }

            content = CodeParser.GetUsingDirectives(content, namespaceReferences);
            content = CodeParser.GetDeclarations(content, namespaces, declarations);
            namespaces.Insert(0, GlobalNamespace);

            Dictionary<int, List<KeyValuePair<int, string>>> declarationsByNamespace = new Dictionary<int, List<KeyValuePair<int, string>>>();
            for (int i = 0; i < namespaces.Count; i++)
            {
                int start = namespaces[i].Key;
                int end = ((namespaces.Count > i + 1) ? namespaces[i + 1].Key : int.MaxValue);

                List<KeyValuePair<int, string>> lst = new List<KeyValuePair<int, string>>();
                foreach (KeyValuePair<int, string> declaration in declarations)
                    if (declaration.Key >= start && declaration.Key < end)
                        if(lst.Where(x => x.Value == declaration.Value).Count() == 0)
                            lst.Add(declaration);

                if (lst.Count > 0)
                    declarationsByNamespace.Add(start, lst);
            }

            declarations.Clear();
            foreach (List<KeyValuePair<int, string>> lst in declarationsByNamespace.Values)
                declarations.AddRange(lst);

            CodeParser.GetReferences(content, typeReferences);

            declarationsByNamespace.Clear();
            for (int i = 0; i < namespaces.Count; i++)
            {
                int start = namespaces[i].Key;
                int end = ((namespaces.Count > i + 1) ? namespaces[i + 1].Key : int.MaxValue);

                List<KeyValuePair<int, string>> lst = new List<KeyValuePair<int, string>>();
                foreach (KeyValuePair<int, string> reference in typeReferences)
                    if (reference.Key >= start && reference.Key < end)
                        if (lst.Where(x => x.Value == reference.Value).Count() == 0)
                            lst.Add(reference);

                if (lst.Count > 0)
                    declarationsByNamespace.Add(start, lst);
            }

            typeReferences.Clear();
            foreach (List<KeyValuePair<int, string>> lst in declarationsByNamespace.Values)
                typeReferences.AddRange(lst);

            for (int i = 0; i < typeReferences.Count; i++)
                if (typeReferences[i].Value.Contains("."))
                {
                    string[] v = typeReferences[i].Value.Split('.');
                    typeReferences[i] = new KeyValuePair<int, string>(typeReferences[i].Key, v[0].Trim());
                }

            namespaces.RemoveAt(0);
        }
        public override void FetchImports()
        {
            
        }

        public KeyValuePair<int, string> GetDeclaringNamespace(KeyValuePair<int, string> item)
        {
            KeyValuePair<int, string> @namespace = GlobalNamespace;
            foreach (KeyValuePair<int, string> namespaceDeclaration in namespaces)
            {
                if (namespaceDeclaration.Key <= item.Key) @namespace = namespaceDeclaration;
                else if (namespaceDeclaration.Key > item.Key) break;
            }
            return @namespace;
        }
        public List<KeyValuePair<int, string>> GetDeclaringNamespaces(string typeName)
        {
            List<KeyValuePair<int, string>> result = new List<KeyValuePair<int, string>>();
            foreach (KeyValuePair<int, string> type in declarations.Where(x => x.Value == typeName))
                result.Add(GetDeclaringNamespace(type));

            return result;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
