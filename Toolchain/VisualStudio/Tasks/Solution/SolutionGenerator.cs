// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Forge.Systems;
using SE.Storage;
using SE.Text;
using SE.App;

namespace SE.Forge.VisualStudio
{
    public abstract class SolutionGenerator : VariadicTask
	{
        private const string AnalyticsNodeName = "{0} Solution";
        private readonly static Type InputPinType = typeof(VisualStudioProject);
        protected readonly VisualStudioVersion version;
        protected readonly UInt32 flag;

        TaskPin[] outputPins;
        public override TaskPin[] OutputPins
        {
            get { return outputPins; }
        }

        public SolutionGenerator(VisualStudioVersion version)
        {
            this.version = version;
            if (version != null)
            {
                outputPins = new TaskPin[]
                {
                    new TaskPin(this, typeof(VisualStudioSolution))
                };
                this.flag = version.Flag.ToString().Fnv32();
            }
        }

        public override void Execute()
        {
            LoadInput();

            VisualStudioDirectory modulesDir = new VisualStudioDirectory(new PathDescriptor("Modules"));
            IEnumerable<VisualStudioProject> projects = inputPins.Select(x => (x.Data as VisualStudioProject));
            VisualStudioSolution solution = new VisualStudioSolution
            (
                new Storage.FileDescriptor
                (
                    new PathDescriptor(Application.WorkerPath), 
                    "{0}.sln", 
                    Path.GetFileNameWithoutExtension
                    (
                        PathDescriptor.GetCommonParent(projects.Where(x => !x.IsModule)).Trim('\\', '/')
                    )
                )
                , version
            );

            bool hasModules = false;
            foreach (VisualStudioProject project in projects)
                if (project.IsModule)
                {
                    project.ParentDirectory = modulesDir;
                    
                    if(!hasModules)
                        hasModules = true;
                }

            if(hasModules)
                solution.Content.Add(modulesDir);

            Application.Log(SeverityFlags.Minimal, "Creating {0} solution {1}", version.Flag.ToString(), solution.File.FullName);

            solution.Projects.AddRange(projects);
            solution.CreateFile();
        }
        
        public override bool VariadicAccepts(TaskPin inputPin)
        {
            return (inputPin is FlaggedPin && (inputPin as FlaggedPin).Flags == flag && InputPinType.IsAssignableFrom(inputPin.DataType));
        }
        public override string ToString()
        {
            return string.Format(AnalyticsNodeName, version.Flag);
        }
    }
}
