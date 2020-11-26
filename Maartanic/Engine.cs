using System;
using System.Collections.Generic;
using System.IO;

namespace Maartanic
{
	class Engine
	{
		private bool executable;
		private string line;
		private int lineIndex;
		private string scriptFile;
		private string[] lineInfo;
		private string entryPoint = "main";
		private int logLevel;
		private StreamReader sr;

		private Dictionary<string, Delegate> predefinedVariables = new Dictionary<string, Delegate>();
		private Dictionary<string, string> localMemory = new Dictionary<string, string>();

		private enum Level
		{
			INFO,
			WARN,
			ERR
		}

		public Engine(string startPos)
		{
			executable = File.Exists(startPos);
			if (!executable)
			{
				Console.WriteLine($"The file {startPos} does not exist.");
				return;
			}

			scriptFile = startPos;
		}

		public bool Executable()
		{
			return executable;
		}

		private bool FindProgram(ref StreamReader sr, ref string line, ref int lineIndex)
		{
			while (((line = sr.ReadLine()) != null) && line != $"DEF {entryPoint}")
			{
				lineIndex++;
			}
			if (line == $"DEF {entryPoint}")
			{
				lineIndex++;
				return true;
			}
			else
				return false; // No entry point "main"!
		}

		private void SendMessage(Level a, string message)
		{
			if ((int) a >= logLevel)
			{
				switch ((int) a)
				{
					case 0:
						Console.WriteLine("MRT INFO line {0}: {1}", lineIndex, message);
						break;
					case 1:
						Console.WriteLine("MRT WARN line {0}: {1}", lineIndex, message);
						break;
					case 2:
						Console.WriteLine("MRT ERR line {0}: {1}", lineIndex, message);
						break;
				}
			}	
		}

		public void StartExecution(int logLevelIN)
		{
			logLevel = logLevelIN;
			lineIndex = 0;
			sr = new StreamReader(scriptFile);
			if (!FindProgram(ref sr, ref line, ref lineIndex))
			{
				// unknown error
				Console.WriteLine("Unknown error");
			}
			while ((line = sr.ReadLine()) != null)
			{
				if (line == null)
				{
					SendMessage(Level.ERR, "Unexpected NULL");
					line = "";
				}
				lineInfo = line.Trim().Split(' ');

				// Check if empty
				if (lineInfo.Length > 0)
				{
					if (lineInfo[0].Length > 0)
					{
						char x = lineInfo[0][0];
						if (x == ';') // A wild comment appeared!
						{
							lineIndex++;
							continue;
						}
					}
				}
				else
				{
					lineIndex++;
					continue;
				}

				string[] args = ExtractArgs(ref lineInfo);
				switch (lineInfo[0].ToUpper())
				{

					case "PRINT":
						if (lineInfo.Length == 1)
						{
							SendMessage(Level.WARN, "No arguments given to print.");
							break;
						}
						Console.Write('\n');
						foreach (string arg in ExtractArgs(ref lineInfo))
						{
							Console.Write(arg);
						}
						break;

					case "OUT":
						if (lineInfo.Length == 1)
						{
							SendMessage(Level.WARN, "No arguments given to OUT. OUT does absolutely nothing without an argument.");
							break;
						}
						foreach (string arg in ExtractArgs(ref lineInfo))
						{
							Console.Write(arg);
						}
						break;

					case "ENDDEF": // Possible end-of-function
						if (lineInfo[1] == entryPoint)
						{
							SendMessage(Level.INFO, "End of definition");
							return;
						}
						else
						{
							SendMessage(Level.ERR, "Unexpected end of definition, expect unwanted side effects.");
						}
						break;

					case "CLEAR":
						if (args.Length != 0)
						{
							for (int i = 0; i < Convert.ToInt32(args[0]); i++)
							{
								Console.SetCursorPosition(0, Console.CursorTop);
								Console.Write(new String(' ', Console.BufferWidth));
								Console.SetCursorPosition(0, Console.CursorTop - 1);
								if (i == Convert.ToInt32(args[0]) - 1)
								{
									Console.SetCursorPosition(0, Console.CursorTop - 1);
								}
							}
							Console.SetCursorPosition(0, Console.CursorTop + 1);
						}
						else
						{
							Console.Clear();
						}
						break;

					case "NEW":
						if (predefinedVariables.ContainsKey(args[0]))
						{
							SendMessage(Level.ERR, $"Variable {args[0]} is a predefined variable and cannot be declared.");
							break;
						}
						if (localMemory.ContainsKey(args[0]))
						{
							SendMessage(Level.WARN, $"Variable {args[0]} already exists.");
							localMemory[args[0]] = args.Length > 1 ? args[1] : "0";
						}
						else
						{
							localMemory.Add(args[0], args.Length > 1 ? args[1] : "0");
						}
						break;

					case "IF":
						{ // local scope to make variables defined here local to this scope!
							if (args.Length > 1)
							{

							}
						}
						break;

					default:
						SendMessage(Level.ERR, $"Instruction {lineInfo[0]} is not recognized.");
						break;
				}
				lineIndex++;
			}
			sr.Close();
		}

		private void LocalMemoryGet(ref List<string> varName, ref int index)
		{
			if (localMemory.ContainsKey(varName[index][1..]))
			{
				varName[index] = localMemory[varName[index][1..]];
			}
			else
			{
				SendMessage(Level.ERR, $"The variable {varName[index][1..]} does not exist.");
				varName[index] = "NULL";
			}
		}

		private string[] ExtractArgs(ref string[] lineInfo)
		{
			string combined = "";
			for (int i = 1; i < lineInfo.Length; i++)
			{
				combined += ' ' + lineInfo[i];
			}
			combined = combined[1..]; // Exclude first space

			// Maybe use RegEx but eh lazy. Escape quotation with a backslash. At least I understand it this way
			// Iterates through it, splits spaces. Things in quotes (") are treated like one block even if there are spaces in between.
			List<string> newCombinedList = new List<string>();
			string newCombined = "";
			bool isInQuotes = false;
			for (int i = 0; i < combined.Length; i++)
			{
				if (combined[i] == '"')
				{
					if (isInQuotes)
					{
						if (combined[i - 1] != '\\')
						{
							isInQuotes = false;
							newCombinedList.Add(newCombined);
							newCombined = "";
							continue;
						}
						else
						{
							newCombined = newCombined[..(newCombined.Length - 1)]; // Exclude the last/escape character
							continue;
						}
					}
					else
					{
						if (i == 0 || combined[i - 1] == ' ')
						{
							isInQuotes = true;
							newCombined = "";
							continue;
						}
					}
				}
				if (isInQuotes)
				{
					newCombined += combined[i];
				}
				else
				{
					if (combined[i] == ' ')
					{
						if (combined[i - 1] != '"')
						{
							newCombinedList.Add(newCombined);
							newCombined = "";
						}
						continue;
					}
					newCombined += combined[i];
				}
				if (i == combined.Length - 1)
				{
					newCombinedList.Add(newCombined);
					newCombined = "";
				}
			}

			for (int i = 0; i < newCombinedList.Count; i++)
			{
				if (newCombinedList[i][0] == '$')
				{
					LocalMemoryGet(ref newCombinedList, ref i);
				}
			}

			return newCombinedList.ToArray();
		}
	}
}
