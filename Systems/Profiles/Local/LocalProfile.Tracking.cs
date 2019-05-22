// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Code.Analytics;
using SE.Threading;
using SE.Storage;
using SE.App;

namespace SE.Forge.Systems.Profiles
{
    public partial class LocalProfile
    {
        private static PooledSpinLock toolGraphLock;
        private static ReferenceGraph toolStackGraph;

        private static void SaveTrackingResults()
        {
            if (toolStackGraph != null)
            {
                PathDescriptor outputPath = Application.GetDeploymentPath(SystemTags.Analytics);
                if(!outputPath.Exists())
                    try
                    {
                        outputPath.Create();
                    }
                    catch { }

                toolStackGraph.Save(outputPath, "ToolStack");
            }
        }
        private static void TrackTask(Task task)
        {
            using (new Scope(toolGraphLock))
            {
                AnalyticsNode root = toolStackGraph.Nodes.Where(x => (x as AnalyticsNode).Id == task.GetHashCode()).FirstOrDefault() as AnalyticsNode;
                if (root == null)
                {
                    root = (toolStackGraph.Add<AnalyticsNode>(null) as AnalyticsNode);
                    root.FillData(task);
                }

                Task childTask = task.Child;
                while (childTask != null)
                {
                    AnalyticsNode child = toolStackGraph.Nodes.Where(x => (x as AnalyticsNode).Id == childTask.GetHashCode()).FirstOrDefault() as AnalyticsNode;
                    if (child == null)
                    {
                        child = (toolStackGraph.Add<AnalyticsNode>(root, null) as AnalyticsNode);
                        child.FillData(childTask);
                    }
                    else child.AddDependency(root);
                    childTask = childTask.Next;
                }

                foreach(TaskPin inPin in task.InputPins)
                    if (inPin.Parent != null)
                    {
                        AnalyticsNode parent = toolStackGraph.Nodes.Where(x => (x as AnalyticsNode).Id == inPin.Parent.Owner.GetHashCode()).FirstOrDefault() as AnalyticsNode;
                        parent.AddDependant(root);
                    }
            }
        }
    }
}
