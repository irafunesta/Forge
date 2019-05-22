// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;

namespace SE.Forge.Systems
{
    public class ConsoleLogSystem : ILogSystem
    {
        const string warningPrefix = "[WARNING]";
        const string warningPattern = warningPrefix + " {0}";
        const string errorPrefix = "[ERROR]";
        const string errorPattern = errorPrefix + " {0}";

        private static readonly ConsoleColor warningColor = ConsoleColor.Yellow;
        private static readonly ConsoleColor errorColor = ConsoleColor.Red;

        ConsoleColor standardColor;
        public ConsoleLogSystem()
        {
            standardColor = Console.ForegroundColor;
        }

        public void Log(string message, params object[] args)
        {
            Console.ForegroundColor = standardColor;
            Console.WriteLine(message, args);
        }
        public void Warning(string message, params object[] args)
        {
            Console.ForegroundColor = warningColor;
            Console.WriteLine(warningPattern, string.Format(message, args));
            Console.ForegroundColor = standardColor;
        }
        public void Error(string message, params object[] args)
        {
            Console.ForegroundColor = errorColor;
            Console.WriteLine(errorPattern, string.Format(message, args));
            Console.ForegroundColor = standardColor;
        }
    }
}
