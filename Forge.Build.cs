// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using SE.Storage;
using SE.App;

namespace Mixins.Forge
{
    public static class Forge
    {
        private readonly static PathDescriptor ForgePath = new PathDescriptor(Application.SdkDirectory, "Tools/Forge");

        [Mixin(typeof(Application))]
        public static void ExcludeFromBuild(dynamic @this, List<PathDescriptor> projects)
        {
            projects.Remove(ForgePath);
        }
    }
}
