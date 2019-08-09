// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SE.Code.Build;
using SE.Storage;
using SE.Text;

namespace SE.Forge.VisualStudio
{
    public class VisualSharpProject : VisualStudioProject
    {
        PathDescriptor propertiesPath;

        public override Guid ProjectTypeGuid
        {
            get { return SharpGuid.ToGuid(); }
        }

        protected List<string> builtInReferences = new List<string>() 
        { 
            "System", 
            "System.Core",
            "Microsoft.Csharp"
        };

        protected Dictionary<FileDescriptor, string> links = new Dictionary<FileDescriptor, string>();
        public Dictionary<FileDescriptor, string> Links
        {
            get { return links; }
        }

        protected string rootNamespace;
        public string RootNamespace
        {
            get { return rootNamespace; }
            set { rootNamespace = value; }
        }

        public VisualSharpProject(VisualStudioVersion version, IEnumerable<FileDescriptor> files, PathDescriptor projectRoot, string name)
            : base(version, files, projectRoot, "{0}.csproj", name)
        { }

        protected void SetConfigHeader(StreamWriter writer)
        {
            writer.WriteLine("  <PropertyGroup>");
            writer.WriteLine("    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>");
            writer.WriteLine("    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>");
            writer.WriteLine("    <ProjectGuid>{0}</ProjectGuid>", projectGuid.ToString("B").ToUpperInvariant());

            string assemblyType;
            switch (outputType)
            {
                case Systems.OutputAssemblyType.Console: assemblyType = "Exe"; break;
                case Systems.OutputAssemblyType.Executable: assemblyType = "WinExe"; break;
                default: assemblyType = "Library"; break;
            }
            writer.WriteLine("    <OutputType>{0}</OutputType>", assemblyType);
            
            writer.WriteLine("    <AppDesignerFolder>Properties</AppDesignerFolder>");
            writer.WriteLine("    <RootNamespace>{0}</RootNamespace>", rootNamespace);
            writer.WriteLine("    <AssemblyName>{0}</AssemblyName>", assemblyName);
            writer.WriteLine("    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>");
            writer.WriteLine("    <OutputPath>{0}</OutputPath>", outputPath.GetRelativePath(location));
            writer.WriteLine("    <WarningLevel>4</WarningLevel>");
            writer.WriteLine("  </PropertyGroup>");
        }
        protected void SetBuildTargets(StreamWriter writer)
        {
            foreach(BuildTarget target in buildTargets)
            {
                writer.WriteLine("  <PropertyGroup Condition=\" '$(Configuration)|$(Platform)' == '{0}|{1}' \">", target.Mode.Name, target.Architecture.ToString());
                writer.WriteLine("    <Optimize>{0}</Optimize>", target.Mode.Optimized);
                writer.WriteLine("    <DebugSymbols>{0}</DebugSymbols>", target.Mode.SymbolExport);

                if (target.Mode.SymbolExport)
                    writer.WriteLine("    <DebugType>full</DebugType>");

                writer.Write("    <DefineConstants>");
                foreach (KeyValuePair<string, object> definitions in target.Platform.Definitions)
                {
                    writer.Write(definitions.Key);
                    writer.Write(";");
                }
                foreach (KeyValuePair<string, object> definitions in target.Mode.Definitions)
                {
                    writer.Write(definitions.Key);
                    writer.Write(";");
                }
                writer.WriteLine("</DefineConstants>");
                writer.WriteLine("    <PlatformTarget>{0}</PlatformTarget>", target.Architecture.ToString());
                writer.WriteLine("  </PropertyGroup>");
            }
        }
        protected void SetReferences(StreamWriter writer)
        {
            writer.WriteLine("  <ItemGroup>");
            PathDescriptor assemblyPath = new PathDescriptor(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
            foreach(string dependency in builtInReferences)
                    writer.WriteLine("    <Reference Include=\"{0}\" />", dependency);
            foreach(FileDescriptor dependency in dependencies)
            {
                if (assemblyPath.Contains(dependency.Location))
                    writer.WriteLine("    <Reference Include=\"{0}\" />", dependency.Name);
                else
                {
                    writer.WriteLine("    <Reference Include=\"{0}\">", dependency.Name);
                    writer.WriteLine("    <HintPath>{0}</HintPath>", dependency.GetRelativePath(location));
                    writer.WriteLine("    </Reference>");
                }
            }
            foreach (VisualSharpProject reference in DependencyProjects)
            {
                writer.WriteLine("    <ProjectReference Include=\"{0}\">", reference.location.GetRelativePath(location));
                writer.WriteLine("      <Project>{0}</Project>", reference.projectGuid.ToString("B").ToUpperInvariant());
                writer.WriteLine("      <Name>{0}</Name>", reference.name);
                writer.WriteLine("    </ProjectReference>");
            }
            writer.WriteLine("  </ItemGroup>");
        }
        protected void SetSourceFiles(StreamWriter writer)
        {
            writer.WriteLine("  <ItemGroup>");
            foreach (FileDescriptor file in files)
                if (file.Extension != "resx")
                {
                    writer.WriteLine("    <Compile Include=\"{0}\" />", file.GetRelativePath(location));
                }
            foreach(KeyValuePair<FileDescriptor, string> link in links)
            {
                writer.WriteLine("    <Compile Include=\"{0}\">", link.Key.GetRelativePath(location));
                writer.WriteLine("      <Link>{0}</Link>", link.Value);
                writer.WriteLine("    </Compile>");
            }
            writer.WriteLine("  </ItemGroup>");
        }
        protected void SetEmbeddedResources(StreamWriter writer)
        {
            writer.WriteLine("  <ItemGroup>");
            foreach (FileDescriptor file in propertiesPath.GetFiles())
                if(file.Extension != "cs")
                {
                    writer.WriteLine("    <EmbeddedResource Include=\"{0}\"/>", file.GetRelativePath(location));
                }
            writer.WriteLine("  </ItemGroup>");
        }

        public override void CreateFile()
        {
            using (FileStream fs = Open(FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                if (propertiesPath == null)
                    propertiesPath = new PathDescriptor(location, "Properties");

                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("<Project ToolsVersion=\"{0}\" DefaultTargets=\"{1}\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">", VisualStudioUtils.GetToolsVersion(version), "Build");

                SetConfigHeader(sw);
                SetBuildTargets(sw);
                SetReferences(sw);
                SetSourceFiles(sw);
                SetEmbeddedResources(sw);

                sw.WriteLine("  <Import Project=\"$(MSBuildToolsPath)\\Microsoft.CSharp.targets\" />");
                sw.WriteLine("</Project>");
                sw.Flush();
            }
        }
    }
}
