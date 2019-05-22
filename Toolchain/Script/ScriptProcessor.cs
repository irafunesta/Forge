// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using SE.Storage;
using SE.Shell;
using SE.Text;
using SE.App;
using System.Text;

namespace SE.Forge.Script
{
    public static class ScriptProcessor
    {
        static ScriptProcessor()
        {
            CommandManager.LoadModule(System.Reflection.Assembly.GetExecutingAssembly());
            Store.ScopeOverride = true;
        }

        private static bool ParseJson(Stream stream)
        {
            long streamPos = stream.Position;
            if (!Store.LoadStoreObject(stream))
            {
                stream.Position = streamPos;
                return false;
            }
            else return true;
        }
        private static bool ParseLine(string line, out bool exitState)
        {
            exitState = (line.Trim().ToLowerInvariant() == "exit");
            if (exitState)
                return true;

            Command com; if (!string.IsNullOrWhiteSpace(CommandParser.ParseLine(line, out com)) && CommandManager.Resolve(ref com))
            {
                int result = com.Execute();
                com.Dispose();

                return (result == Application.SuccessReturnCode);
            }
            else return false;
        }

        public static bool Execute(FileDescriptor file)
        {
            using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                do
                {
                    fs.SkipUtf8(" \t\r\n");
                    if (fs.PeekUtf8() != "{" || !ParseJson(fs))
                    {
                        long streamPos = fs.Position;
                        long length = fs.FindAnyUtf8(Environment.NewLine);

                        if (length == 0)
                            return false;

                        fs.Position = streamPos;
                        bool exitState; if (!ParseLine(fs.Read(length), out exitState))
                            return false;

                        if (exitState)
                            return true;
                    }
                }
                while (!fs.Eof());
            return true;
        }
        public static ScriptResult GetResult(PathDescriptor location)
        {
            ScriptResult result = new ScriptResult(location, Store.GetStoreObject());
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("{}")))
                Store.LoadStoreObject(ms);

            return result;
        }
    }
}
