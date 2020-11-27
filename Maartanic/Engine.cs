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
			INF,
			WRN,
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
						Console.Write("\nMRT INF line {0}: {1}", lineIndex, message);
						break;
					case 1:
						Console.Write("\nMRT WRN line {0}: {1}", lineIndex, message);
						break;
					case 2:
						Console.Write("\nMRT ERR line {0}: {1}", lineIndex, message);
						break;
				}
			}	
		}

		public bool LineCheck(ref string[] lineInfo, ref int lineIndex)
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
						return true;
					}
				}
			}
			else
			{
				lineIndex++;
				return true;
			}
			return false;
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
				if (LineCheck(ref lineInfo, ref lineIndex))
				{
					continue;
				}

				string[] args = ExtractArgs(ref lineInfo);
				switch (lineInfo[0].ToUpper())
				{

					case "PRINT":
						if (lineInfo.Length == 1)
						{
							SendMessage(Level.WRN, "No arguments given to print.");
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
							SendMessage(Level.WRN, "No arguments given to OUT. OUT does absolutely nothing without an argument.");
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
							SendMessage(Level.INF, "End of definition");
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
							SendMessage(Level.WRN, $"Variable {args[0]} already exists.");
							localMemory[args[0]] = args.Length > 1 ? args[1] : "0";
						}
						else
						{
							localMemory.Add(args[0], args.Length > 1 ? args[1] : "0");
						}
						break;

					case "IF":
						{ // local scope to make variables defined here local to this scope!
							string statement = args.Length > 1 ? args[1] : args[0];
							bool result;
							bool invertStatement = args.Length > 1 && args[0] == "NOT";
							if (statement == "1" || statement == "true")
							{
								result = true;
							}
							else
							{
								result = localMemory.ContainsKey(statement);
							}
							result = invertStatement ? !result : result;
							if (!result)
							{
								int scope = 0;
								bool success = false;
								int ifLineIndex = 0;
								string[] cLineInfo = null;
								StreamReader ifsr = new StreamReader(scriptFile);
								while ((line = ifsr.ReadLine()) != null)
								{
									if (LineCheck(ref cLineInfo, ref ifLineIndex))
									{
										continue;
									}
									if (ifLineIndex > lineIndex)
									{										
										if ((cLineInfo[0].ToUpper() == "ELSE" || cLineInfo[0].ToUpper() == "ENDIF") && scope == 0)
										{
											success = true;
											break;
										}
										if (cLineInfo[0].ToUpper() == "IF")
										{
											scope++;
										}
										if (cLineInfo[0].ToUpper() == "ENDIF")
										{
											scope--;
										}
									}
									ifLineIndex++;
								}
								if (success)
								{
									for (int i = lineIndex; i < ifLineIndex; i++)
									{
										if ((line = sr.ReadLine()) == null)
										{
											break; // safety protection?
										}
									}
									lineIndex = ifLineIndex;
								}
								else
								{
									SendMessage(Level.ERR, "Could not find a spot to jump to.");
								}
							}
						}
						break;

					case "ENDIF": // To be ignored
						break;

					case "ELSE": // TODO When this appears, the engine should jump to ENDIF.
						{
							int scope = 0;
							bool success = false;
							int endifLineIndex = 0;
							string[] cLineInfo = null;
							StreamReader endifsr = new StreamReader(scriptFile);
							while ((line = endifsr.ReadLine()) != null)
							{
								if (LineCheck(ref cLineInfo, ref endifLineIndex))
								{
									continue;
								}
								if (endifLineIndex > lineIndex)
								{
									if (cLineInfo[0].ToUpper() == "ENDIF" && scope == 0)
									{
										success = true;
										break;
									}
									if (cLineInfo[0].ToUpper() == "IF")
									{
										scope++;
									}
									if (cLineInfo[0].ToUpper() == "ENDIF")
									{
										scope--;
									}
								}
								endifLineIndex++;
							}
							if (success)
							{
								for (int i = lineIndex; i < endifLineIndex; i++)
								{
									if ((line = sr.ReadLine()) == null)
									{
										break; // safety protection?
									}
								}
								lineIndex = endifLineIndex;
							}
							else
							{
								SendMessage(Level.ERR, "Could not find a spot to jump to.");
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

		private void LocalMemoryGet(ref string varName)
		{
			if (varName[0] == '$')
			{
				if (localMemory.ContainsKey(varName[1..]))
				{
					varName = localMemory[varName[1..]];
				}
				else
				{
					SendMessage(Level.ERR, $"The variable {varName[1..]} does not exist.");
					varName = "NULL";
				}
			}
		}

		private string[] ExtractArgs(ref string[] lineInfo)
		{
			string combined = "";
			for (int i = 1; i < lineInfo.Length; i++)
			{
				combined += ' ' + lineInfo[i];
			}
			if (combined.Length < 1)
			{
				return null;
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

			{ // Make scope
				string tmp;
				for (int i = 0; i < newCombinedList.Count; i++)
				{
					tmp = newCombinedList[i];
					LocalMemoryGet(ref tmp);
				}
			}

			return newCombinedList.ToArray();
		}
	}
}
