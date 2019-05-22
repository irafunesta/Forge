// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using SE.Code.Build;
using SE.Storage;
using SE.Text;
using SE.Forge.Systems;
using SE.App;

namespace SE.Forge.VisualStudio
{
    public class VisualCppProject : VisualStudioProject
    {
        public override Guid ProjectTypeGuid
        {
            get { return CppGuid.ToGuid(); }
        }

        public VisualCppProject(VisualStudioVersion version, IEnumerable<FileDescriptor> files, PathDescriptor projectRoot, string name)
            : base(version, files, projectRoot, "{0}.vcxproj", name)
        { }

        protected void SetBuildTargets(StreamWriter writer)
        {
            writer.WriteLine("  <ItemGroup Label=\"ProjectConfigurations\">");

            foreach (BuildTarget target in buildTargets)
            {
                writer.WriteLine("    <ProjectConfiguration Include=\"{0}|{1}\">", target.Mode.Name, target.Architecture);
                writer.WriteLine("      <Configuration>{0}</Configuration>", target.Mode.Name);
                writer.WriteLine("      <Platform>{0}</Platform>", target.Architecture);
                writer.WriteLine("    </ProjectConfiguration>");
            }

            writer.WriteLine("  </ItemGroup>");
        }
        protected void SetProjectFiles(StreamWriter writer)
        {
            List<FileDescriptor> sourceFiles = new List<FileDescriptor>();
            List<FileDescriptor> assemblyFiles = new List<FileDescriptor>();

            writer.WriteLine("  <ItemGroup>");

            foreach (FileDescriptor file in files)
                switch (file.Extension)
                {
                    case "h":
                    case "hpp": writer.WriteLine("    <ClInclude Include=\"$(ProjectDir){0}\" />", file.GetRelativePath(location)); break;
                    case "c":
                    case "cpp": sourceFiles.Add(file); break;
                    case "S":
                    case "asm": assemblyFiles.Add(file); break;
                }

            writer.WriteLine("  </ItemGroup>");
            writer.WriteLine("  <ItemGroup>");

            foreach (FileDescriptor file in sourceFiles)
                writer.WriteLine("    <ClCompile Include=\"$(ProjectDir){0}\" />", file.GetRelativePath(location));

            writer.WriteLine("  </ItemGroup>");
            writer.WriteLine("  <ItemGroup>");

            foreach (FileDescriptor file in assemblyFiles)
                writer.WriteLine("    <MASM Include=\"$(ProjectDir){0}\" />", file.GetRelativePath(location));

            writer.WriteLine("  </ItemGroup>");
        }
        protected void SetTargetConfiguration(StreamWriter writer)
        {
            writer.WriteLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.Default.props\" />");

            string projectType;
            switch (outputType)
            {
                case OutputAssemblyType.Console:
                case OutputAssemblyType.Executable: projectType = "Application"; break;
                case OutputAssemblyType.Library: projectType = "DynamicLibrary"; break;
                case OutputAssemblyType.Static:
                default: projectType = "StaticLibrary"; break;
            }
            string extension;
            switch (outputType)
            {
                case OutputAssemblyType.Console:
                case OutputAssemblyType.Executable: extension = ".exe"; break;
                case OutputAssemblyType.Library: extension = ".dll"; break;
                case OutputAssemblyType.Static:
                default: extension = ".lib"; break;
            }
            foreach (BuildTarget target in buildTargets)
            {
                writer.WriteLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='{0}|{1}'\" Label=\"Configuration\">", target.Mode.Name, target.Architecture);
                writer.WriteLine("    <ConfigurationType>{0}</ConfigurationType>", projectType);
                writer.WriteLine("    <UseDebugLibraries>{0}</UseDebugLibraries>", (target.Mode.Optimized) ? "false" : "true");
                writer.WriteLine("    <PlatformToolset>{0}</PlatformToolset>", VisualStudioUtils.GetToolsetVersion(version));
                writer.WriteLine("  </PropertyGroup>");

                writer.WriteLine("  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='{0}|{1}'\">", target.Mode.Name, target.Architecture);
                writer.WriteLine("    <OutDir>{0}</OutDir>", outputPath.GetRelativePath(location));
                writer.WriteLine("    <IntDir>{0}$(ProjectName)\\</IntDir>", Application.BuildDirectory.GetRelativePath(location));
                writer.WriteLine("    <TargetName>{0}</TargetName>", assemblyName);
                writer.WriteLine("    <TargetExt>{0}</TargetExt>", extension);
                writer.WriteLine("  </PropertyGroup>");

                writer.WriteLine("  <ImportGroup Label=\"PropertySheets\" Condition=\"'$(Configuration)|$(Platform)'=='{0}|{1}'\">", target.Mode.Name, target.Architecture);
                writer.WriteLine("    <Import Project=\"$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\" Condition=\"exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')\" Label=\"LocalAppDataPlatform\" />");
                writer.WriteLine("  </ImportGroup>");

                writer.WriteLine("  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='{0}|{1}'\">", target.Mode.Name, target.Architecture);
                writer.WriteLine("    <ClCompile>");

                writer.Write("      <AdditionalIncludeDirectories>");
                writer.Write("$(ProjectDir);");
                foreach (VisualStudioProject reference in DependencyProjects)
                {
                    writer.Write("$(ProjectDir)");
                    writer.Write(reference.Location.GetRelativePath(location));
                    writer.Write(";");
                }
                writer.WriteLine("</AdditionalIncludeDirectories>");

                writer.Write("      <PreprocessorDefinitions>");
                foreach (KeyValuePair<string, object> definitions in target.Mode.Definitions)
                {
                    writer.Write(definitions.Key);
                    writer.Write(";");
                }
                writer.WriteLine("</PreprocessorDefinitions>");
                writer.WriteLine("      <WarningLevel>Level4</WarningLevel>");
                writer.WriteLine("      <TreatWarningAsError>{0}</TreatWarningAsError>", (target.Mode.WarningAsError) ? "true" : "false");
                writer.WriteLine("      <ExceptionHandling>false</ExceptionHandling>");

                if(target.Mode.Optimized)
                    writer.WriteLine("      <Optimization>MaxSpeed</Optimization>");

                writer.WriteLine("    </ClCompile>");
                writer.WriteLine("    <Link>");

                HashSet<UInt32> directories = new HashSet<UInt32>();
                writer.Write("      <AdditionalLibraryDirectories>");
                foreach (VisualStudioProject reference in DependencyProjects)
                {
                    UInt32 id = reference.OutputPath.GetAbsolutePath().Fnv32();
                    if (!directories.Contains(id))
                    {
                        writer.Write("$(ProjectDir)\\");
                        writer.Write(reference.OutputPath.GetRelativePath(location));
                        writer.Write(";");
                        directories.Add(id);
                    }
                }
                writer.WriteLine("</AdditionalLibraryDirectories>");
                writer.Write("      <AdditionalDependencies>");
                foreach (VisualStudioProject reference in DependencyProjects)
                {
                    string depExtension;
                    switch (reference.OutputType)
                    {
                        case OutputAssemblyType.Console:
                        case OutputAssemblyType.Executable: depExtension = ".exe"; break;
                        case OutputAssemblyType.Library: depExtension = ".dll"; break;
                        case OutputAssemblyType.Static:
                        default: depExtension = ".lib"; break;
                    }

                    writer.Write("{0}{1}", reference.AssemblyName, depExtension);
                    writer.Write(";");
                }
                foreach (FileDescriptor external in dependencies)
                {
                    writer.Write(external.FullName);
                    writer.Write(";");
                }
                writer.WriteLine("</AdditionalDependencies>");

                writer.WriteLine("      <GenerateDebugInformation>{0}</GenerateDebugInformation>", target.Mode.SymbolExport);
                writer.WriteLine("    </Link>");

                writer.WriteLine("  </ItemDefinitionGroup>");
            }

            writer.WriteLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.props\" />");
            writer.WriteLine("  <ImportGroup Label=\"ExtensionSettings\">");
            writer.WriteLine("    <Import Project=\"$(VCTargetsPath)\\BuildCustomizations\\masm.props\" />");
            writer.WriteLine("  </ImportGroup>");
        }
        protected void SetTargetImports(StreamWriter writer)
        {
            writer.WriteLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.targets\" />");
            writer.WriteLine("  <ImportGroup Label=\"ExtensionTargets\">");
            writer.WriteLine("    <Import Project=\"$(VCTargetsPath)\\BuildCustomizations\\masm.targets\" />");
            writer.WriteLine("  </ImportGroup>");
        }

        protected void SetFilterDirectories(PathDescriptor root, StreamWriter writer)
        {
            Filter filter = new Filter();
            FilterToken token = filter.Add("...");
            filter.Add(token, ".*").Exclude = true;

            foreach (PathDescriptor path in root.FindDirectories(filter))
            {
                writer.WriteLine("    <Filter Include=\"{0}\">", path.GetRelativePath(location).Trim('\\', '/'));
                writer.WriteLine("      <UniqueIdentifier>" + Guid.NewGuid().ToString("B").ToUpperInvariant() + "</UniqueIdentifier>");
                writer.WriteLine("    </Filter>");

                SetFilterDirectories(path, writer);
            }
        }
        protected void SetFilterFiles(StreamWriter writer)
        {
            List<FileDescriptor> sourceFiles = new List<FileDescriptor>();
            List<FileDescriptor> assemblyFiles = new List<FileDescriptor>();

            writer.WriteLine("  <ItemGroup>");

            foreach (FileDescriptor file in files)
                switch (file.Extension)
                {
                    case "h":
                    case "hpp":
                        {
                            if (location != file.Location && location.Contains(file.Location))
                            {
                                writer.WriteLine("    <ClInclude Include=\"$(ProjectDir){0}\">", file.GetRelativePath(location));
                                writer.WriteLine("      <Filter>{0}</Filter>", file.Location.GetRelativePath(location).Trim('\\', '/'));
                                writer.WriteLine("    </ClInclude>");
                            }
                            else writer.WriteLine("    <ClInclude Include=\"$(ProjectDir){0}\" />", file.GetRelativePath(location).Trim('\\', '/'));
                        }
                        break;
                    case "c":
                    case "cpp": sourceFiles.Add(file); break;
                    case "asm": assemblyFiles.Add(file); break;
                }

            writer.WriteLine("  </ItemGroup>");
            writer.WriteLine("  <ItemGroup>");

            foreach (FileDescriptor file in sourceFiles)
                if (location != file.Location && location.Contains(file.Location))
                {
                    writer.WriteLine("    <ClCompile Include=\"$(ProjectDir){0}\">", file.GetRelativePath(location));
                    writer.WriteLine("      <Filter>{0}</Filter>", file.Location.GetRelativePath(location).Trim('\\', '/'));
                    writer.WriteLine("    </ClCompile>");
                }
                else writer.WriteLine("    <ClCompile Include=\"$(ProjectDir){0}\" />", file.GetRelativePath(location).Trim('\\', '/'));

            writer.WriteLine("  </ItemGroup>");
            writer.WriteLine("  <ItemGroup>");

            foreach (FileDescriptor file in assemblyFiles)
                if (location != file.Location && location.Contains(file.Location))
                {
                    writer.WriteLine("    <MASM Include=\"$(ProjectDir){0}\">", file.GetRelativePath(location));
                    writer.WriteLine("      <Filter>{0}</Filter>", file.Location.GetRelativePath(location).Trim('\\', '/'));
                    writer.WriteLine("    </MASM>");
                }
                else writer.WriteLine("    <MASM Include=\"$(ProjectDir){0}\" />", file.GetRelativePath(location).Trim('\\', '/'));

            writer.WriteLine("  </ItemGroup>");
        }

        public override void CreateFile()
        {
            if (!Application.BuildDirectory.Exists())
                Application.BuildDirectory.CreateHidden();

            using (FileStream fs = Open(FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sw.WriteLine("<Project ToolsVersion=\"{0}\" DefaultTargets=\"{1}\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">", VisualStudioUtils.GetToolsVersion(version), "Build");
                sw.WriteLine("  <PropertyGroup Label=\"Globals\">");
                sw.WriteLine("    <ProjectGuid>{0}</ProjectGuid>", projectGuid.ToString("B").ToUpperInvariant());
                sw.WriteLine("  </PropertyGroup>");

                SetBuildTargets(sw);
                SetProjectFiles(sw);
                SetTargetConfiguration(sw);
                SetTargetImports(sw);

                sw.WriteLine("</Project>");
                sw.Flush();
            }
            using (FileStream fs = new FileDescriptor(location, "{0}.filters", FullName).Open(FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sw.WriteLine("<Project ToolsVersion=\"{0}\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">", VisualStudioUtils.GetToolsVersion(version));

                sw.WriteLine("  <ItemGroup>");
                SetFilterDirectories(location, sw);
                sw.WriteLine("  </ItemGroup>");
                SetFilterFiles(sw);

                sw.WriteLine("</Project>");
                sw.Flush();
            }
        }
    }
}
