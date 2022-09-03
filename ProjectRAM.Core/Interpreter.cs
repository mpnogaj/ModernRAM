﻿using ProjectRAM.Core.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using static ProjectRAM.Core.Utilities;

namespace ProjectRAM.Core
{
	public class Interpreter
	{
		private readonly Dictionary<string, int> _jumpMap;
		private readonly List<Command> _program;
		private string _currentInput = "";
		private const string UninitializedValue = "?";


		public event EventHandler<WriteFromTapeEventArgs>? WriteToOutputTape;
		public event EventHandler<ReadFromTapeEventArgs>? ReadFromInputTape;
		public event EventHandler? ProgramFinished;
		
		public Interpreter(List<Command> program)
		{
			_program = program;
			_jumpMap = MapLabels();
			Memory = new Dictionary<string, string>()
			{
				{"0", UninitializedValue}
			};
			MaxMemory = new Dictionary<string, string>()
			{
				{"0", UninitializedValue}
			};
			Complexity = new Complexity(this);
		}

		public Complexity Complexity { get; private set; }
		internal Dictionary<string, string> MaxMemory { get; private set; }
		internal Dictionary<string, string> Memory { get; private set; }

		public Dictionary<string, string> RunCommands() => RunCommands(CancellationToken.None);

		public Dictionary<string, string> RunCommands(CancellationToken cancellationToken)
		{
			for (var i = 0; i < _program.Count; i++)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					ProgramFinished?.Invoke(this, EventArgs.Empty);
					cancellationToken.ThrowIfCancellationRequested();
				}
				
				if (RunCommand(ref i))
				{
					return Memory;
				}
			}
			ProgramFinished?.Invoke(this, EventArgs.Empty);
			return Memory;
		}

		private string GetValue(Command c, string formattedArg)
		{
			if (c.ArgumentType == ArgumentType.Const)
			{
				return formattedArg;
			}

			if (!Memory.ContainsKey(formattedArg))
			{
				throw new UninitializedCellException(c.Line);
			}

			return Memory[formattedArg];
		}

		private int Jump(string lbl, int index)
		{
			if (_jumpMap.ContainsKey(lbl))
			{
				return _jumpMap[lbl] - 1;
			}
			throw new UnknownLabelException(_program[index].Line, lbl);
		}

		private Dictionary<string, int> MapLabels()
		{
			var i = 0;
			Dictionary<string, int> labels = new();
			foreach (var command in _program)
			{
				if (!string.IsNullOrWhiteSpace(command.Label))
				{
					labels.Add(command.Label, i);
				}
				i++;
			}
			return labels;
		}

		private bool RunCommand(ref int i)
		{
			var command = _program[i];
			//bool exists;
			string arg = command.FormattedArg();
			if (command.ArgumentType == ArgumentType.IndirectAddress)
			{
				arg = Memory[arg];
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

					#endregion ExceptionHandling

					i = Jump(arg, i);
					break;

				case CommandType.Jgtz:

					#region ExceptionHandling

					if (command.ArgumentType != ArgumentType.Label)
					{
						throw new ArgumentIsNotValidException(command.Line);
					}

					#endregion ExceptionHandling

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

					#endregion ExceptionHandling

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

					#endregion Exception handling

					string val;

					if (ReadFromInputTape == null)
					{
						throw new InputTapeEmptyException(i);
					}

					var eventArgs = new ReadFromTapeEventArgs();
					ReadFromInputTape?.Invoke(this, eventArgs);

					val = eventArgs.Input ?? throw new InputTapeEmptyException(i);

					_currentInput = val;
					SetMemory(arg, val);
					break;

				case CommandType.Write:

					#region ExceptionHandling

					if (command.ArgumentType == ArgumentType.Label)
					{
						throw new ArgumentIsNotValidException(command.Line);
					}

					#endregion ExceptionHandling

					string output;

					if (command.ArgumentType == ArgumentType.Const)
					{
						output = arg;
						//_outputTape.Enqueue(arg);
					}
					else
					{
						if (!Memory.ContainsKey(arg))
						{
							throw new UninitializedCellException(command.Line);
						}

						output = Memory[arg];
						//_outputTape.Enqueue(Memory[arg]);
					}

					WriteToOutputTape?.Invoke(this, new WriteFromTapeEventArgs(output));

					break;

				case CommandType.Store:

					#region ExceptionHandling

					if (command.ArgumentType == ArgumentType.Const ||
						command.ArgumentType == ArgumentType.Label)
					{
						throw new ArgumentIsNotValidException(command.Line);
					}

					#endregion ExceptionHandling

					SetMemory(arg, Memory["0"]);
					break;

				case CommandType.Load:

					#region ExceptionHandling

					if (command.ArgumentType == ArgumentType.Label)
					{
						throw new ArgumentIsNotValidException(command.Line);
					}

					#endregion ExceptionHandling

					SetMemory("0", GetValue(command, arg));
					break;

				case CommandType.Add:

					#region ExceptionHandling

					if (command.ArgumentType == ArgumentType.Label)
					{
						throw new ArgumentIsNotValidException(command.Line);
					}

					if (Memory["0"] == UninitializedValue)
					{
						throw new AccumulatorEmptyException(command.Line);
					}

					#endregion ExceptionHandling

					SetMemory("0", BigInteger
							.Add(BigInteger.Parse(Memory["0"]),
								BigInteger.Parse(GetValue(command, arg))).ToString());
					break;

				case CommandType.Sub:

					#region ExceptionHandling

					if (command.ArgumentType == ArgumentType.Label)
					{
						throw new ArgumentIsNotValidException(command.Line);
					}

					if (Memory["0"] == UninitializedValue)
					{
						throw new AccumulatorEmptyException(command.Line);
					}

					#endregion ExceptionHandling

					SetMemory("0", BigInteger
							.Subtract(BigInteger.Parse(Memory["0"]),
								BigInteger.Parse(GetValue(command, arg))).ToString());
					break;

				case CommandType.Mult:

					#region ExceptionHandling

					if (command.ArgumentType == ArgumentType.Label)
					{
						throw new ArgumentIsNotValidException(command.Line);
					}

					if (Memory["0"] == UninitializedValue)
					{
						throw new AccumulatorEmptyException(command.Line);
					}

					#endregion ExceptionHandling

					SetMemory("0", BigInteger
							.Multiply(BigInteger.Parse(Memory["0"]),
								BigInteger.Parse(GetValue(command, arg))).ToString());
					break;

				case CommandType.Div:

					#region ExceptionHandling

					if (command.ArgumentType == ArgumentType.Label)
					{
						throw new ArgumentIsNotValidException(command.Line);
					}

					if (Memory["0"] == UninitializedValue)
					{
						throw new AccumulatorEmptyException(command.Line);
					}

					if (GetValue(command, arg) == "0")
					{
						throw new DivideByZeroException();
					}

					#endregion ExceptionHandling

					SetMemory("0", BigInteger
						.Divide(BigInteger.Parse(Memory["0"]),
							BigInteger.Parse(GetValue(command, arg))).ToString());
					break;
			}

			if (command.CommandType != CommandType.Null && command.CommandType != CommandType.Unknown)
			{
				Complexity.UpdateTimeComplexity(command, _currentInput);
			}
			return false;
		}

		private void SetMemory(string key, string value)
		{
			if (!Memory.ContainsKey(key))
			{
				Memory[key] = value;
				MaxMemory[key] = value;
			}
			else
			{
				string oldValue = Memory[key];
				Memory[key] = value;
				MaxMemory[key] = oldValue == "?" ? value : Max(MaxMemory[key], value);
			}
		}
	}
}