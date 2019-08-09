// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Forge.Systems.Tasks;
using SE.Forge.Systems.Pins;
using SE.Forge.Systems;
using SE.Reflection;
using SE.Parallel;
using SE.Storage;
using SE.Mixins;
using SE.App;

namespace SE.Forge
{
    public class ForgeSetup : Task
    {
        private const string AnalyticsNodeName = "Forge-Root";
        private readonly static Type MixinType = typeof(Mixin);

        public override TaskPin[] InputPins
        {
            get { return TaskPin.Empty; }
        }

        List<TaskPin> outputPins = new List<TaskPin>();
        public override TaskPin[] OutputPins
        {
            get { return outputPins.ToArray(); }
        }

        public override bool IsPending
        {
            get { return (TaskGraph.Tasks > 1); }
        }
        public ForgeSetup()
        { }

        public override void Execute()
        {
            if (Application.DisplayManual) ForgeTool.DisplayManual();
            else
            {
                TaskGraph.SetEndPoints(Application.Profile.GetEndPoints());
                AppDomain.CurrentDomain.GetAssemblies().ParallelFor((assembly) =>
                {
                    foreach (Type task in assembly.GetTypes<ITaskPrototype>())
                        if (!task.IsAbstract)
                            try
                            {
                                TaskPrototypes.Add(task.CreateInstance<ITaskPrototype>());
                            }
                            catch (Exception er)
                            {
                                if (Application.LogSeverity == SeverityFlags.Full) Application.Error(SeverityFlags.None, "{0}{1}{2}", er.Message, Environment.NewLine, er.StackTrace);
                                else Application.Error(SeverityFlags.None, er.Message);
                            }
                });

                foreach (FileDescriptor file in Application.GetBuildFiles())
                    outputPins.Add(new FlaggedPin(this, file, SystemTags.SealedSetup));

                UInt32 flag; if (outputPins.Count > 0) flag = SystemTags.SealedSetup;
                else flag = SystemTags.ForgeSetup;
                foreach (PathDescriptor path in Application.GetBuildPaths())
                    outputPins.Add(new FlaggedPin(this, path, flag));

                if (outputPins.Count == 0)
                    Application.Error(SeverityFlags.None, "Build paths not valid");
            }
        }

        public override string ToString()
        {
            return AnalyticsNodeName;
        }
    }
}
