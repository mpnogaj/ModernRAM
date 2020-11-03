﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace Common
{
    /// <summary>
    /// Class used to create different collections (code, input tape)
    /// </summary>
    public static class Creator
    {
        /// <summary>
        /// Create list of commands
        /// </summary>
        /// <param name="lines">Code lines</param>
        /// <returns>List of Command objects.</returns>
        public static List<Command> CreateCommandList(StringCollection lines)
        {
            List<Command> commands = new List<Command>();
            long i = 0;
            foreach (string line in lines)
            {
                commands.Add(new Command(line, ++i));
            }
            return commands;
        }

        /// <summary>
        /// Convert List of commands to code lines
        /// </summary>
        /// <param name="commands">List of commands</param>
        /// <returns>Code lines</returns>
        public static StringCollection CommandListToStringCollection(List<Command> commands)
        {
            StringCollection sc = new StringCollection();
            foreach (var command in commands)
            {
                sc.Add(command.ToString());
            }
            return sc;
        }

        /// <summary>
        /// Create command list from file
        /// </summary>
        /// <param name="pathToFile">Path to .RAMCode file</param>
        /// <returns>List of commands</returns>
        public static List<Command> CreateCommandList(string pathToFile)
        {
            List<Command> commands = new List<Command>();
            using (StreamReader sr = new StreamReader(pathToFile))
            {
                long i = 0;
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    commands.Add(new Command(line, ++i));
                }
            }
            return commands;
        }

        /// <summary>
        /// Create input tape from file
        /// </summary>
        /// <param name="pathToFile">Path to file</param>
        /// <returns>Queue of strings</returns>
        public static Queue<string> CreateInputTapeFromFile(string pathToFile)
        {
            Queue<string> inputTape = new Queue<string>();
            using (StreamReader sr = new StreamReader(pathToFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    inputTape.Enqueue(line);
                }
            }

            return inputTape;
        }

        /// <summary>
        /// Create input tape from given string
        /// </summary>
        /// <param name="line">Input tape string</param>
        /// <returns>Queue of strings</returns>
        public static Queue<string> CreateInputTapeFromString(string line)
        {
            if (line == string.Empty)
            {
                return null;
            }

            return new Queue<string>(line.Split(' '));
        }
    }
}
