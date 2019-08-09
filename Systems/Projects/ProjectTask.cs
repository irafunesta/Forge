// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Storage;
using SE.App;

namespace SE.Forge.Systems
{
    public abstract class ProjectTask : Task, ITaskPrototype
    {
        public virtual bool Enabled
        {
            get { return true; }
        }

        public bool ExclusiveUse
        {
            get { return false; }
        }

        public abstract string[] FileExtensions
        {
            get;
        }

        protected TaskPin[] inputPins;
        public override TaskPin[] InputPins
        {
            get { return inputPins; }
        }

        protected TaskPin[] outputPins;
        public override TaskPin[] OutputPins
        {
            get { return outputPins; }
        }

        public ProjectTask(UInt32 projectIdentifier)
        {
            inputPins = new TaskPin[]
            {
                new ExclusivePin(this, typeof(PathDescriptor), SystemTags.ForgeSetup)
            };
            outputPins = new TaskPin[]
            {
                new FlaggedPin(this, typeof(Project), projectIdentifier)
            };
        }

        public override void Execute()
        {
            LoadInput();

            try
            {
                List<FileSystemDescriptor> files = Application.GetProjectFiles(inputPins[0].Data as PathDescriptor, FileExtensions);
                if (files.Count > 0) outputPins[0].Data = CreateProject(null, files);
                else Cancel();
            }
            catch (Exception e)
            {
                Cancel();
                throw e;
            }
        }

        protected abstract Project CreateProject(object arguments, List<FileSystemDescriptor> files);
        protected void Cancel()
        {
            foreach (TaskPin pin in outputPins)
                pin.Dispose();

            outputPins = TaskPin.Empty;
            child = null;
        }

        public bool VariadicAccepts(TaskPin inputPin)
        {
            return false;
        }
        public abstract Task CreateInstance();
    }
}
