// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using SE.Storage;
using SE.Config;
using SE.App;

namespace SE.Forge.Config
{
    public delegate bool TypeConverterCallback(string value, out object result);
    public class StorageConverter : ITypeConverter
    {
        private static Dictionary<Type, TypeConverterCallback> converter = new Dictionary<Type, TypeConverterCallback>();
        private static bool DetermineByValue(string value, out object result)
        {
            if (File.Exists(Path.GetFullPath(value))) return CreateFileDescriptor(value, out result);
            else return CreatePathDescriptor(value, out result);
        }
        private static bool CreateFileDescriptor(string value, out object result)
        {
            string dir = Path.GetDirectoryName(value);
            if (string.IsNullOrEmpty(dir))
                dir = Application.WorkerPath;

            result = new FileDescriptor(new PathDescriptor(dir), Path.GetFileName(value));
            return true;
        }
        private static bool CreatePathDescriptor(string value, out object result)
        {
            result = new PathDescriptor(value);
            return true;
        }
        private static bool CreateStorageDescriptor(string value, out object result)
        {
            string dir = Path.GetDirectoryName(value);
            if (string.IsNullOrEmpty(dir))
                dir = Application.WorkerPath;

            result = new StorageDescriptor(new PathDescriptor(dir), Path.GetFileName(value));
            return true;
        }

        static StorageConverter()
        {
            converter.Add(typeof(StorageDescriptor), CreateStorageDescriptor);
            converter.Add(typeof(FileSystemDescriptor), DetermineByValue);
            converter.Add(typeof(FileDescriptor), CreateFileDescriptor);
            converter.Add(typeof(PathDescriptor), CreatePathDescriptor);
        }

        public bool TryParseValue(Type memberType, string value, out object result)
        {
            TypeConverterCallback callback; if (!converter.TryGetValue(memberType, out callback) || !callback(value, out result))
            {
                result = null;
                return false;
            }
            else return true;
        }
    }
}
