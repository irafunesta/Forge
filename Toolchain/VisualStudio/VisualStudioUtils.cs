// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using SE.Storage;

namespace SE.Forge.VisualStudio
{
    public static class VisualStudioUtils
    {
        private static bool TryGetRegistryKey(string entry, string key, out string value)
        {
            if (TryGetRegistryKeyInternal("HKEY_CURRENT_USER\\SOFTWARE\\" + entry, key, out value)) 
                return true;
            else if (TryGetRegistryKeyInternal("HKEY_LOCAL_MACHINE\\SOFTWARE\\" + entry, key, out value)) 
                return true;
            else if (TryGetRegistryKeyInternal("HKEY_CURRENT_USER\\SOFTWARE\\Wow6432Node\\" + entry, key, out value)) 
                return true;
            else if (TryGetRegistryKeyInternal("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" + entry, key, out value)) 
                return true;
            else 
                return false;
        }
        private static bool TryGetRegistryKeyInternal(string entry, string key, out string value)
        {
            value = (Microsoft.Win32.Registry.GetValue(entry, key, null) as string);
            if (!string.IsNullOrEmpty(value)) return true;
            else return false;
        }

        public static string GetToolsVersion(VisualStudioVersion version)
        {
            switch (version.Flag)
            {
                case VisualStudioVersionFlag.VisualStudio2010:
                case VisualStudioVersionFlag.VisualStudio2012:
                    return "4.0";
                case VisualStudioVersionFlag.VisualStudio2013:
                    return "12.0";
                case VisualStudioVersionFlag.VisualStudio2015:
                    return "14.0";
                case VisualStudioVersionFlag.VisualStudio2017:
                    return "15.0";
            }
            return string.Empty;
        }
        public static string GetToolsetVersion(VisualStudioVersion version)
        {
            switch (version.Flag)
            {
                case VisualStudioVersionFlag.VisualStudio2010:
                    return "v100";
                case VisualStudioVersionFlag.VisualStudio2012:
                    return "v110";
                case VisualStudioVersionFlag.VisualStudio2013:
                    return "v120";
                case VisualStudioVersionFlag.VisualStudio2015:
                    return "v140";
                case VisualStudioVersionFlag.VisualStudio2017:
                    return "v141";
            }
            return string.Empty;
        }

        public static VisualStudioVersion GetEditorVersion(VisualStudioVersionFlag flag)
        {
            foreach (VisualStudioVersion version in FindEditorVersions())
                if (version.Flag == flag)
                    return version;

            return null;
        }

        public static bool FindEditorVersion(out VisualStudioVersion version)
        {
            List<VisualStudioVersion> versions = FindEditorVersions();
            if (versions.Count != 0) version = (versions[0] as VisualStudioVersion);
            else version = null;

            return (version != null);
        }
        public static List<VisualStudioVersion> FindEditorVersions()
        {
            string[] versions = new string[] { "15.0", "14.0", "12.0", "11.0", "10.0" };
            List<VisualStudioVersion> result = new List<VisualStudioVersion>();
            string path;

            foreach (string version in versions)
                if (TryGetRegistryKey("Microsoft\\VisualStudio\\SxS\\VS7", version, out path))
                    result.Add(new VisualStudioVersion(version, new PathDescriptor(Path.GetFullPath(path))));

            return result;
        }

        private static string FormatMessage(string path, int line, int column, string prefix, string id, string message)
        {
            string location = " ";
            if (line > 0)
                location = string.Format("({0},{1})", line, column);
            string code = string.Empty;
            if (!string.IsNullOrWhiteSpace(prefix))
                code = string.Format("{0} {1}", prefix, id);

            return string.Format("{0}{1}: {2}: {3}",
            path,
            location,
            code,
            message);
        }
        public static string CreateInfoMessage(string path, int line, int column, string message)
        {
            return FormatMessage(path, line, column, string.Empty, string.Empty, message);
        }
        public static string CreateWarningMessage(string path, int line, int column, string id, string message)
        {
            return FormatMessage(path, line, column, "warning", id, message);
        }
        public static string CreateErrorMessage(string path, int line, int column, string id, string message)
        {
            return FormatMessage(path, line, column, "error", id, message);
        }
    }
}
