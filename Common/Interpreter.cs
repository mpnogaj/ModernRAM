﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Common
{
    public static class Interpreter
    {
        /// <summary>
        /// Variables whitch can be read after executin program
        /// Cleared and initialized at the begging of the program
        /// </summary>
        public static List<Command> Program = new List<Command>();
        public static Dictionary<string, string> Memory = new Dictionary<string, string>();
        public static Queue<string> InputTape = new Queue<string>();
        public static Queue<string> OutputTape = new Queue<string>();
        public static List<Command> ExecutedCommands = new List<Command>();

        /// <summary>
        /// Funkcja do skakania. Preszukuje wszystkie komendy i jeżeli znajdzie etykiete to skacze do niej.
        /// </summary>
        /// <param name="c">Lista komend</param>
        /// <param name="lbl">Etykieta do wyszukania</param>
        /// <param name="index">Numer linii</param>
        /// <returns>Indeks komendy do której skoczyć</returns>
        private static int Jump(string lbl, int index)
        {
            int i = 0;
            foreach (Command command in Program)
            {
                if (command.Label == lbl)
                {
                    //w pętli for znowu zwięszke 'i' więc się wyrówna.
                    return i - 1;
                }
                i++;
            }
            throw new LabelDoesntExistExcpetion(Program[index].Line, lbl);
        }

        private static string GetValue(Command c, string formatedArg, Dictionary<string, string> memory)
        {
            if (c.ArgumentType == ArgumentType.Const)
            {
                return formatedArg;
            }
            else
            {
                bool exists = memory.ContainsKey(formatedArg);
                if (!exists)
                {
                    throw new CellDoesntExistException(c.Line);
                }

                return memory[formatedArg];
            }
        }

        /// <summary>
        /// Funkcja symulująca wykonanie wszystkich poleceń
        /// </summary>
        /// <param name="commands">Lista komend do wykonania</param>
        /// <param name="inputTape">Taśma wejściowa</param>
        /// <param name="token">Token anulowania</param>
        /// <returns>Taśmę wyjścia, pamięć</returns>
        public static void RunCommands(List<Command> commands, Queue<string> inputTape, CancellationToken token)
        {
            //Dictionary<string, string> memory = new Dictionary<string, string>();
            Program = commands;
            Memory.Clear();
            Memory.Add("0", string.Empty);
            OutputTape.Clear();
            InputTape.Clear();
            InputTape = inputTape;
            ExecutedCommands.Clear();
            for (int i = 0; i < commands.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                if (RunCommand(ref i)) break;
            }
        }

        /// <summary>
        /// Run single command
        /// </summary>
        /// <param name="commands">List of another commands (for Jump)</param>
        /// <param name="inputTape">Input tape</param>
        /// <param name="outputTape">Output tape</param>
        /// <param name="memory">Memory</param>
        /// <param name="i">Index of current command in list</param>
        /// <returns>Should end program</returns>
        public static bool RunCommand(ref int i)
        {
            Command command = Program[i];
            ExecutedCommands.Add(command);
            bool exists;
            BigInteger value;
            //argument
            command.ArgumentType = Command.GetArgumentType(command.Argument);
            string arg = command.Argument;
            if (command.ArgumentType == ArgumentType.Const || command.ArgumentType == ArgumentType.IndirectAddress)
            {
                arg = arg.Substring(1);
                if (command.ArgumentType == ArgumentType.IndirectAddress)
                {
                    arg = Memory[arg];
                }
            }

            switch (command.CommandType)
            {
                case CommandType.Halt:
                    return true;
                case CommandType.Jump:

                    #region ExceptionHandling

                    if (command.ArgumentType != ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    i = Jump(arg, i);
                    break;
                case CommandType.Jgtz:

                    #region ExceptionHandling

                    if (command.ArgumentType != ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    if (Memory["0"] != "" &&
                        Memory["0"][0] != '-' &&
                        Memory["0"] != "0")
                    {
                        i = Jump(arg, i);
                    }

                    break;
                case CommandType.Jzero:

                    #region ExceptionHandling

                    if (command.ArgumentType != ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    if (Memory["0"] != "" &&
                        Memory["0"] == "0")
                    {
                        i = Jump(arg, i);
                    }

                    break;
                case CommandType.Read:

                    #region Exception handling

                    if (command.ArgumentType != ArgumentType.DirectAddress &&
                        command.ArgumentType != ArgumentType.IndirectAddress)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    if (InputTape == null || InputTape.Count <= 0)
                    {
                        throw new InputTapeEmptyException(command.Line);
                    }

                    #endregion
                    //index = GetMemoryIndex(Memory, argInt);
                    exists = Memory.ContainsKey(arg);
                    string val = InputTape.Dequeue();

                    if (exists == false)
                    {
                        Memory.Add(arg, val);
                    }
                    else
                    {
                        Memory[arg] = val;
                    }

                    break;
                case CommandType.Write:

                    #region ExceptionHandling

                    if (command.ArgumentType == ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    if (command.ArgumentType == ArgumentType.Const)
                    {
                        OutputTape.Enqueue(arg);
                    }
                    else
                    {
                        exists = Memory.ContainsKey(arg);
                        if (exists == false)
                        {
                            throw new CellDoesntExistException(command.Line);
                        }

                        OutputTape.Enqueue(Memory[arg]);
                    }

                    break;
                case CommandType.Store:

                    #region ExceptionHandling

                    if (command.ArgumentType == ArgumentType.Const ||
                        command.ArgumentType == ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    //index = GetMemoryIndex(Memory, argInt);
                    exists = Memory.ContainsKey(arg);
                    if (!exists)
                    {
                        Memory.Add(arg, Memory["0"]);
                    }
                    else
                    {
                        Memory[arg] = Memory["0"];
                    }

                    break;
                case CommandType.Load:

                    #region ExceptionHandling

                    if (command.ArgumentType == ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    Memory["0"] = GetValue(command, arg, Memory);

                    break;
                case CommandType.Add:

                    #region ExceptionHandling

                    if (command.ArgumentType == ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    value = BigInteger.Parse(GetValue(command, arg, Memory));

                    Memory["0"] = BigInteger
                        .Add(BigInteger.Parse(Memory["0"] ?? throw new AccumulatorEmptyException(command.Line)), value).ToString();
                    break;
                case CommandType.Sub:

                    #region ExceptionHandling

                    if (command.ArgumentType == ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    value = BigInteger.Parse(GetValue(command, arg, Memory));

                    Memory["0"] = BigInteger
                        .Subtract(BigInteger.Parse(Memory["0"] ?? throw new AccumulatorEmptyException(command.Line)), value).ToString();
                    break;
                case CommandType.Mult:

                    #region ExceptionHandling

                    if (command.ArgumentType == ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    value = BigInteger.Parse(GetValue(command, arg, Memory));

                    Memory["0"] = BigInteger
                        .Multiply(BigInteger.Parse(Memory["0"] ?? throw new AccumulatorEmptyException(command.Line)), value).ToString();
                    break;
                case CommandType.Div:

                    #region ExceptionHandling

                    if (command.ArgumentType == ArgumentType.Label)
                    {
                        throw new ArgumentIsNotValidException(command.Line);
                    }

                    #endregion

                    value = BigInteger.Parse(GetValue(command, arg, Memory));

                    Memory["0"] = BigInteger
                        .Divide(BigInteger.Parse(Memory["0"] ?? throw new AccumulatorEmptyException(command.Line)), value).ToString();
                    break;
            }
            return false;
        }
    }
}
