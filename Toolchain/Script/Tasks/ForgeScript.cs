// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Forge.Systems;
using SE.Storage;
using SE.App;

namespace SE.Forge.Script
{
    public class ForgeScript : Task, ITaskPrototype
    {
        private const string AnalyticsNodeName = "Script '{0}'";

        public bool Enabled
        {
            get { return true; }
        }

        TaskPin[] inputPins;
        public override TaskPin[] InputPins
        {
            get { return inputPins; }
        }

        List<TaskPin> outputPins;
        public override TaskPin[] OutputPins
        {
            get { return outputPins.ToArray(); }
        }

        public ForgeScript()
        {
            inputPins = new TaskPin[] 
            {
                new ExclusivePin(this, typeof(FileDescriptor), SystemTags.SealedSetup)
            };
            outputPins = new List<TaskPin>();
        }

        public override void Execute()
        {
            LoadInput();

            ScriptResult result;
            try
            {
                if (!ScriptProcessor.Execute(inputPins[0].Data as FileDescriptor))
                {
                    Application.Error(SeverityFlags.Full, "Script '{0}' failed execution", (inputPins[0].Data as FileDescriptor).Name);
                    return;
                }
            }
            finally
            {
                result = ScriptProcessor.GetResult((inputPins[0].Data as FileDescriptor).Location);
            }

            foreach (PathDescriptor project in result.Projects)
                outputPins.Add(new FlaggedPin(this, project, SystemTags.SealedSetup));
        }

        public bool VariadicAccepts(TaskPin inputPin)
        {
            return false;
        }
        public Task CreateInstance()
        {
            return new ForgeScript();
        }

        public override string ToString()
        {
            if (!inputPins[0].HasData) return base.ToString();
            else return string.Format(AnalyticsNodeName, (inputPins[0].Data as FileDescriptor).FullName);
        }
    }
}
