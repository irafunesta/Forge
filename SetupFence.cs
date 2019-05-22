// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Forge.Systems;
using SE.Storage;
using SE.App;

namespace SE.Forge
{
    public class SetupFence : VariadicTask
    {
        private const string AnalyticsNodeName = "Sync";
        private readonly static SetupFence instance;

        public static SetupFence Instance
        {
            get { return instance; }
        }

        static SetupFence()
        {
            instance = new SetupFence();
        }

        public override bool Enabled
        {
            get { return Application.GetBuildFiles().GetEnumerator().MoveNext(); }
        }

        public override bool IsPending
        {
            get { return (TaskGraph.Tasks > 1); }
        }

        List<TaskPin> outputPins;
        public override TaskPin[] OutputPins
        {
            get { return outputPins.ToArray(); }
        }

        public SetupFence()
        {
            outputPins = new List<TaskPin>();
        }

        public override void Execute()
        {
            LoadInput();

            foreach (TaskPin inputPin in inputPins)
                outputPins.Add(new FlaggedPin(this, inputPin.Data, SystemTags.ForgeSetup));

            TaskPrototypes.Remove(this);
        }

        public override bool VariadicAccepts(TaskPin inputPin)
        {
            return (inputPin is FlaggedPin && inputPin.DataType == typeof(PathDescriptor) && (inputPin as FlaggedPin).Flags == SystemTags.SealedSetup);
        }
        public override Task CreateInstance()
        {
            return instance;
        }

        public override string ToString()
        {
            return AnalyticsNodeName;
        }
    }
}
