using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

//TODO check if readonly really is necessary
#pragma warning disable IDE0044

namespace Maartanic
{
	class Engine
	{
		internal delegate void EventHandler(object sender, EventArgs args);
		internal event EventHandler ThrowEvent = delegate { };
		internal void SomethingHappened() => ThrowEvent(this, new EventArgs());

		internal StreamReader sr;
		private int logLevel;

		private bool executable;
		internal string scriptFile;
		internal string entryPoint = "main";

		internal string line;
		internal int lineIndex;
		private string[] lineInfo;

		private bool compareOutput = false;
		private bool keyOutput = false;
		internal string returnedValue = "NULL";
		internal bool redraw = true;

		//internal Mode applicationMode = Mode.VSB; //TODO Make this static inside Program class to avoid having to toss it around when performing instructions
		private DateTime startTime = DateTime.UtcNow;

		private Dictionary<string, Delegate> predefinedVariables = new Dictionary<string, Delegate>();
		internal Dictionary<string, string> localMemory = new Dictionary<string, string>();

		// Level: Used in SendMessage method to indicate the message level as info, warning or error.
		internal enum Level
		{
			INF,
			WRN,
			ERR
		}

		// Mode: Used in applicationMode to let the engine know to enable extended functions not (yet) included in the VSB Engine.
		internal enum Mode
		{
			EXTENDED,
			VSB
		}

		private static T Parse<T>(string input)
		{
			return Program.Parse<T>(input);
		}

		// FillPredefinedList(): Fills the predefinedVariables array with Delegates (Functions) to accommodate for the system in VSB
		private void FillPredefinedList()
		{
			Dictionary<string, Func<string>> toBeAdded = new Dictionary<string, Func<string>>()
			{
				{ "ww",         () => Console.WindowWidth.ToString() },
				{ "wh",         () => Console.WindowHeight.ToString() },
				{ "cmpr",       () => compareOutput.ToString() },
				{ "projtime",   () => (DateTime.UtcNow - startTime).TotalSeconds.ToString() },
				{ "projid",     () => "0" },
				{ "user",       () => "*guest" },
				{ "ver",        () => "1.3" },
				{ "ask",        () => Console.ReadLine() },
				{ "graphics",   () => (Program.applicationMode == Mode.EXTENDED).ToString().ToLower() },
				{ "thour",      () => DateTime.UtcNow.Hour.ToString() },
				{ "tminute",    () => DateTime.UtcNow.Minute.ToString() },
				{ "tsecond",    () => DateTime.UtcNow.Second.ToString() },
				{ "tyear",      () => DateTime.UtcNow.Year.ToString() },
				{ "tmonth",     () => DateTime.UtcNow.Month.ToString() },
				{ "tdate",      () => DateTime.UtcNow.Day.ToString() },
				{ "tdow",       () => ((int)DateTime.UtcNow.DayOfWeek).ToString() },
				{ "key",        () => keyOutput.ToString() },
				{ "ret",        () => returnedValue },
				{ "mx",         () => "0" }, //NOTICE mouse x and y are not supported
				{ "my",         () => "0" },
				{ "redraw",     () => redraw.ToString() }
			};

			foreach (KeyValuePair<string, Func<string>> a in toBeAdded)
			{
				predefinedVariables.Add(a.Key, a.Value);
			}

		}

		// Engine(): Class constructor, returns if given file does not exist.
		public Engine(string startPos)
		{
			executable = File.Exists(startPos);
			if (!executable)
			{
				Console.WriteLine($"The file {startPos} does not exist.");
				return;
			}
			FillPredefinedList();
			scriptFile = startPos;
		}

		// Engine() OVERLOADED: Specify your entry point
		public Engine(string startPos, string customEntryPoint)
		{
			entryPoint = customEntryPoint; // default is main
			executable = File.Exists(startPos);
			if (!executable)
			{
				Console.WriteLine($"The file {startPos} does not exist.");
				return;
			}
			FillPredefinedList();
			scriptFile = startPos;
		}

		// Executable(): Returns whether or not it is ready to be executed based on Engine()'s result.
		public bool Executable()
		{
			return executable;
		}

		// FindProgram(): Basically -jumps- to a method declaration in code
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

		// SendMessage(): Logs a message to the console with a level, including line of execution.
		internal void SendMessage(Level a, string message)
		{
			if ((int)a >= logLevel)
			{
				switch ((int)a)
				{
					case 0:
						Console.Write($"\nMRT INF line {lineIndex}: {message}");
						break;
					case 1:
						Console.Write($"\nMRT WRN line {lineIndex}: {message}");
						break;
					case 2:
						Console.Write($"\nMRT ERR line {lineIndex}: {message}");
						break;
				}
			}
		}

		// LineCheck(): Splits the text into an array for further operations.
		public bool LineCheck(ref string[] lineInfo, ref int lineIndex, bool disable = false)
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
					if (x == '[')
					{
						if (!disable)
						{
							ExtractEngineArgs(ref lineInfo);
						}
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

		// StartExecution(): "Entry point" to the program. This goes line by line, and executes instructions.
		public string StartExecution(int logLevelIN, bool jump = false, int jumpLine = 0)
		{
			logLevel = logLevelIN;
			lineIndex = 0;
			sr = new StreamReader(scriptFile);
			if (jump)
			{
				JumpToLine(ref sr, ref line, ref lineIndex, ref jumpLine);
			}
			else if (!FindProgram(ref sr, ref line, ref lineIndex))
			{
				// unknown error
				Console.WriteLine("Unknown error");
			}
			while ((line = sr.ReadLine()) != null)
			{
				lock(Program.internalShared.SyncRoot)
				{
					if (Program.internalShared[0] == "FALSE")
					{
						SendMessage(Level.ERR, $"Internal process has to close due to {Program.internalShared[1]}.");
						Program.Exit("1");
					}
				}

				lineIndex++;

				if (LineCheck(ref lineInfo, ref lineIndex))
				{
					continue;
				}

				string[] args = ExtractArgs(ref lineInfo);
				switch (lineInfo[0].ToUpper())
				{
					case "": // Empty
						break;

					case "PRINT":
						if (lineInfo.Length == 1)
						{
							SendMessage(Level.WRN, "No arguments given to print.");
							break;
						}
						Console.Write('\n');
						foreach (string arg in args)
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
							return "0";
						}
						else
						{
							SendMessage(Level.ERR, "Unexpected end of definition, expect unwanted side effects.");
						}
						break;

					case "CLEAR":
						if (args != null)
						{
							int imax = Program.Parse<int>(args[0]);
							Console.SetCursorPosition(0, Console.CursorTop);
							Console.Write(new string(' ', Console.BufferWidth));
							for (int i = 1; i < imax; i++)
							{
								Console.SetCursorPosition(0, Console.CursorTop - 2);
								Console.Write(new string(' ', Console.BufferWidth));
								if (i == imax - 1)
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
						if (predefinedVariables.ContainsKey(args[0][1..]))
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
							bool invertStatement = args.Length > 1 && args[0].ToUpper() == "NOT";
							if (statement == "1" || statement.ToUpper() == "TRUE")
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
								JumpToLine(ref ifsr, ref line, ref ifLineIndex, ref lineIndex);
								while ((line = ifsr.ReadLine()) != null)
								{
									ifLineIndex++;
									if (LineCheck(ref cLineInfo, ref ifLineIndex, true))
									{
										continue;
									}
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

					case "ELSE":
						StatementJumpOut("ENDIF", "IF");
						break;

					case "SET":
						if (localMemory.ContainsKey(args[0]))
						{
							localMemory[args[0]] = args[1];
						}
						else
						{
							SendMessage(Level.ERR, $"The variable {args[0]} does not exist.");
						}
						break;

					case "DEL":
						if (localMemory.ContainsKey(args[0]))
						{
							localMemory.Remove(args[0]);
						}
						else
						{
							SendMessage(Level.WRN, $"Tried removing a non-existing variable {args[0]}.");
						}
						break;

					case "ADD": // Pass arg[2] if it exists else ignore it
						{
							string tmp = Convert.ToString(MathOperation('+', args[0], args[1], args.Length > 2 ? args[2] : null));
							SetVariable(args[0], ref tmp);
						}
						break;

					case "SUB":
						{
							string tmp = Convert.ToString(MathOperation('-', args[0], args[1], args.Length > 2 ? args[2] : null));
							SetVariable(args[0], ref tmp);
						}
						break;

					case "DIV":
						{
							string tmp = Convert.ToString(MathOperation('/', args[0], args[1], args.Length > 2 ? args[2] : null));
							SetVariable(args[0], ref tmp);
						}
						break;

					case "MUL":
						{
							string tmp = Convert.ToString(MathOperation('*', args[0], args[1], args.Length > 2 ? args[2] : null));
							SetVariable(args[0], ref tmp);
						}
						break;

					case "CMPR":
						{
							compareOutput = Compare(ref args);
						}
						break;

					case "ROUND":
						{
							string varName, num1IN, sizeIN;
							if (args.Length > 2)
							{
								varName = args[0];
								num1IN = args[1];
								sizeIN = args[2];
							}
							else
							{
								varName = args[0];
								num1IN = '$' + varName;
								LocalMemoryGet(ref num1IN);
								sizeIN = args[1];
							}
							if (!decimal.TryParse(num1IN, out decimal num1))
							{
								SendMessage(Level.ERR, "Malformed number found.");
							}
							if (!int.TryParse(sizeIN, out int size))
							{
								SendMessage(Level.ERR, "Malformed number found.");
							}
							string output = Math.Round(num1, size).ToString();
							SetVariable(args[0], ref output);
						}
						break;

					case "COLRGBTOHEX":
					case "RGBTOHEX": // RGBTOHEX preferred instruction for Maartanic
						{
							string varName = args[0];
							if (!int.TryParse(args[1], out int r)) { SendMessage(Level.ERR, "Malformed number found."); }
							if (!int.TryParse(args[2], out int g)) { SendMessage(Level.ERR, "Malformed number found."); }
							if (!int.TryParse(args[3], out int b)) { SendMessage(Level.ERR, "Malformed number found."); }
							string output = $"{r:X2}{g:X2}{b:X2}";
							SetVariable(varName, ref output);
						}
						break;

					case "HEXTORGB":
						{
							string[] varNames = new string[3] { args[0], args[1], args[2] };
							string[] colorsOut = new string[3];
							string color = args[3];
							color = color[0] == '#' ? color : '#' + color;
							System.Drawing.Color colorOutput;
							try
							{
								colorOutput = System.Drawing.ColorTranslator.FromHtml(color);
							}
							catch (ArgumentException)
							{
								SendMessage(Level.ERR, "Malformed hexadecimal number found.");
								colorOutput = new System.Drawing.Color();
							}
							colorsOut[0] = colorOutput.R.ToString();
							colorsOut[1] = colorOutput.G.ToString();
							colorsOut[2] = colorOutput.B.ToString();
							for (int i = 0; i < 3; i++)
							{
								SetVariable(varNames[i], ref colorsOut[i]);
							}
						}
						break;

					case "RAND":
						{
							string varName = args[0];
							if (!int.TryParse(args[1], out int lowerLim)) { SendMessage(Level.ERR, "Malformed number found."); }
							if (!int.TryParse(args[2], out int higherLim)) { SendMessage(Level.ERR, "Malformed number found."); }
							Random generator = new Random();
							string output = generator.Next(lowerLim, higherLim + 1).ToString();
							SetVariable(varName, ref output);
						}
						break;

					case "SIZE":
						{
							string varName = args[0], output;

							if (args.Length > 1)
							{
								output = args[1].Length.ToString();
							}
							else
							{
								output = '$' + varName;
								LocalMemoryGet(ref output);
								output = output.Length.ToString();
							}
							SetVariable(varName, ref output);
						}
						break;

					case "ABS":
						{
							string varName = args[0], output;
							decimal n;
							if (args.Length > 1)
							{
								if (!decimal.TryParse(args[1], out n)) { SendMessage(Level.ERR, "Malformed number found."); }
								output = Math.Abs(n).ToString();
							}
							else
							{
								output = '$' + varName;
								LocalMemoryGet(ref output);
								if (!decimal.TryParse(output, out n)) { SendMessage(Level.ERR, "Malformed number found."); }
								output = Math.Abs(n).ToString();
							}
							SetVariable(varName, ref output);
						}
						break;

					case "MIN":
						PerformOp("min", args[0], args[1], args.Length > 2 ? args[2] : null);
						break;
					case "MAX":
						PerformOp("max", args[0], args[1], args.Length > 2 ? args[2] : null);
						break;
					case "CON":
						{
							string a, b, output;
							if (args.Length > 2)
							{
								a = args[1];
								b = args[2];
							}
							else
							{
								a = '$' + args[0];
								LocalMemoryGet(ref a);
								b = args[1];
							}
							output = a + b;
							SetVariable(args[0], ref output);
						}
						break;

					case "KEY":
						{
							if (!char.TryParse(args[0], out char key)) { key = 'x'; SendMessage(Level.ERR, "Malformed character found."); }
							ConsoleKeyInfo cki;
							if (Console.KeyAvailable)
							{
								cki = Console.ReadKey();
								keyOutput = cki.KeyChar == key;
							}
							else
							{
								keyOutput = false;
							}
						}
						break;

					case "HLT":
						SendMessage(Level.INF, "HLT");
						Program.Exit("2");
						break; //NOTICE Unreachable code but IDE complains for some reason

					case "SUBSTR":
						{
							string input, output;
							int start, len;
							if (args.Length > 3)
							{
								input = args[1];
								if (!int.TryParse(args[2], out start)) { SendMessage(Level.ERR, "Malformed number found."); }
								if (!int.TryParse(args[3], out len)) { SendMessage(Level.ERR, "Malformed number found."); }
							}
							else
							{
								input = '$' + args[0];
								LocalMemoryGet(ref input);
								if (!int.TryParse(args[1], out start)) { SendMessage(Level.ERR, "Malformed number found."); }
								if (!int.TryParse(args[2], out len)) { SendMessage(Level.ERR, "Malformed number found."); }
							}
							output = input.Substring(start, len);
							SetVariable(args[0], ref output);
						}
						break;

					case "CHARAT":
						{
							string input, output = "NULL";
							int index;
							if (args.Length > 2)
							{
								input = args[1];
								if (!int.TryParse(args[2], out index)) { index = -1; SendMessage(Level.ERR, "Malformed number found."); }
							}
							else
							{
								input = '$' + args[0];
								LocalMemoryGet(ref input);
								if (!int.TryParse(args[1], out index)) { index = -1; SendMessage(Level.ERR, "Malformed number found."); }
							}
							if (index < 0 || index >= input.Length)
							{
								SendMessage(Level.ERR, $"Index {index} is out of bounds.");
							}
							else
							{
								output = input[index].ToString();
							}
							SetVariable(args[0], ref output);
						}
						break;

					case "TRIM":
						{
							string input;
							if (args.Length > 1)
							{
								input = args[1];
							}
							else
							{
								input = '$' + args[0];
								LocalMemoryGet(ref input);
							}
							input = System.Text.RegularExpressions.Regex.Replace(input.Trim(), @"\s+", " "); // Trim and remove duplicate spaces
							SetVariable(args[0], ref input);
						}
						break;

					case "DO":
					case "CALL":
						{
							Engine E = new Engine(scriptFile, args[0]);
							if (E.Executable())
							{
								returnedValue = E.StartExecution(logLevel);
							}
							else
							{
								SendMessage(Level.ERR, "Program was not executable.");
							}
						}
						break;

					case "RET":
						if (args != null && args.Length > 0)
						{
							return args[0];
						}
						return "0"; // Manual close

					case "RPLC":
						{
							string output, input, old, _new; // "new" is a C# keyword so use "_new" instead!
							if (args.Length > 3)
							{
								input = args[1];
								old = args[2];
								_new = args[3];
							}
							else
							{
								input = '$' + args[0];
								LocalMemoryGet(ref input);
								old = args[1];
								_new = args[2];
							}
							output = input.Replace(old, _new);
							SetVariable(args[0], ref output);
						}
						break;

					case "COUNT":
						{
							string output, input, value;
							if (args.Length > 2)
							{
								input = args[1];
								value = args[2];
							}
							else
							{
								input = '$' + args[0];
								LocalMemoryGet(ref input);
								value = args[1];
							}
							output = Regex.Matches(input, value).Count.ToString();
							SetVariable(args[0], ref output);
						}
						break;

					case "FIND":
						{
							string output, input, value;
							if (args.Length > 2)
							{
								input = args[1];
								value = args[2];
							}
							else
							{
								input = '$' + args[0];
								LocalMemoryGet(ref input);
								value = args[1];
							}
							output = input.IndexOf(value).ToString();
							SetVariable(args[0], ref output);
						}
						break;

					case "COS": // (Trigonometric) math functions
					case "SIN":
					case "TAN":
					case "ACOS":
					case "ASIN":
					case "ATAN":
					case "LOG":
					case "MATH_LN":
					case "EPOW":
					case "TENPOW":
					case "TORAD":
					case "TODEG":
					case "FLR":
					case "CEIL":
						MathFunction(lineInfo[0].ToUpper(), args[0], args.Length > 1 ? args[1] : null);
						break;

					case "PUSH":
						Program.stack.Push(args[0]);
						break;

					case "POP":
						{
							string output;
							if (Program.stack.HasNext())
							{
								Program.stack.Pop(out output);
							}
							else
							{
								output = "NULL";
								SendMessage(Level.ERR, "Stack was empty and could not be popped from.");
							}
							SetVariable(args[0], ref output);
						}
						break;

					case "QPUSH":
						Program.queue.Enqueue(args[0]);
						break;

					case "QPOP":
						{
							string output;
							if (Program.queue.HasNext())
							{
								Program.queue.Dequeue(out output);
							}
							else
							{
								output = "NULL";
								SendMessage(Level.ERR, "Queue was empty and could not be dequeued from.");
							}
							SetVariable(args[0], ref output);
						}
						break;

					case "ALOC":
						{
							if (!int.TryParse(args[0], out int amount)) { SendMessage(Level.ERR, "Malformed number found."); }
							for (int i = 0; i < amount; i++)
							{
								Program.memory.Add("0");
							}
						}
						break;

					case "FREE":
						{
							if (!int.TryParse(args[0], out int amount)) { SendMessage(Level.ERR, "Malformed number found."); }
							for (int i = 0; i < amount; i++)
							{
								if (!Program.memory.Exists(0))
								{
									SendMessage(Level.WRN, "Tried freeing memory that doesn't exist.");
									continue;
								}
								else
								{
									Program.memory.Remove(1);
								}
							}
						}
						break;

					case "SETM":
						{
							if (!int.TryParse(args[0], out int address)) { SendMessage(Level.ERR, "Malformed number found."); break; }
							SetMemoryAddr(address, args[1]);
						}
						break;

					case "GETM":
						{
							if (!int.TryParse(args[1], out int address)) { SendMessage(Level.ERR, "Malformed number found."); break; }
							Program.memory.Get(address, out string output);
							SetVariable(args[0], ref output);
						}
						break;

					case "REDRAWOK":
						redraw = false;
						break;

					default:
						if (Program.applicationMode == Mode.EXTENDED) // Enable extended instruction set
						{
							string output = Program.extendedMode.Instructions(this, ref lineInfo, ref args);
							if (output != null)
							{
								return output;
							}
						}
						else
						{
							SendMessage(Level.ERR, $"Unrecognized instruction \"{lineInfo[0]}\". (VSB)");
						}
						break;
				}
			}
			sr.Close(); // Close StreamReader after execution
			return returnedValue;
		}

		// StatementJumpOut(): Jumps out of the statement.
		internal void StatementJumpOut(string endNaming, string startNaming)
		{
			int scope = 0;
			bool success = false;
			int whileLineIndex = 0;
			string[] cLineInfo = null;
			StreamReader endifsr = new StreamReader(scriptFile);
			while ((line = endifsr.ReadLine()) != null)
			{
				whileLineIndex++;
				if (LineCheck(ref cLineInfo, ref whileLineIndex))
				{
					continue;
				}
				if (whileLineIndex > lineIndex)
				{
					if (cLineInfo[0].ToUpper() == endNaming && scope == 0)
					{
						success = true;
						break;
					}
					if (cLineInfo[0].ToUpper() == startNaming)
					{
						scope++;
					}
					if (cLineInfo[0].ToUpper() == endNaming)
					{
						scope--;
					}
				}

			}
			if (success)
			{
				SendMessage(Level.INF, "Continuing at line " + whileLineIndex);
				for (int i = lineIndex; i < whileLineIndex; i++)
				{
					if ((line = sr.ReadLine()) == null)
					{
						break; // safety protection?
					}
				}
				lineIndex = whileLineIndex;
			}
			else
			{
				SendMessage(Level.ERR, $"Could not jump to end of {startNaming}.");
			}
		}

		// JumpToLine(): Jumps to a line in the streamreader.
		internal void JumpToLine(ref StreamReader sr, ref string line, ref int lineIndex, ref int jumpLine)
		{
			while (((line = sr.ReadLine()) != null) && lineIndex < jumpLine-1)
			{
				lineIndex++;
			}
			if (lineIndex >= jumpLine-1)
			{
				lineIndex++;
			}
			else
			{
				SendMessage(Level.ERR, $"Unable to jump to line {jumpLine}");
			}
		}

		// SetMemoryAddr(): Sets a given memory address to the given value. 
		private void SetMemoryAddr(int address, string value)
		{
			address = Program.applicationMode == Mode.VSB ? address - 1 : address;
			if (!Program.memory.Exists(address))
			{
				SendMessage(Level.ERR, $"Memory address {address} does not exist.");
			}
			else
			{
				Program.memory.Set(address, value);
			}
		}

		// ToRadians(): Converts a given angle in degrees to radians with a limited amount of accuracy
		private double ToRadians(double input)
		{
			return 0.01745329251 * input;
		}

		// ToDegrees(): Converts a given angle in radians to degrees with a limited amount of accuracy
		private double ToDegrees(double input)
		{
			return 57.2957795131 * input;
		}

		// MathFunction(): Method merges multiple cases in the big switch of StartExecution().
		private void MathFunction(string function, string destination, string number)
		{
			double dnumA;
			if (number == null)
			{
				string tmp = '$' + destination;
				LocalMemoryGet(ref tmp);
				if (!double.TryParse(tmp, out dnumA)) { SendMessage(Level.ERR, "Malformed number found."); }
			}
			else
			{
				if (!double.TryParse(number, out dnumA)) { SendMessage(Level.ERR, "Malformed number found."); }
			}
			double result;
			switch (function)
			{
				case "COS": // To radians, use it, and back to degrees.
					result = Math.Cos(ToRadians(dnumA));
					break;

				case "SIN":
					result = Math.Sin(ToRadians(dnumA));
					break;

				case "TAN":
					result = Math.Tan(ToRadians(dnumA));
					break;

				case "ACOS":
					result = Math.Acos(ToRadians(dnumA));
					break;

				case "ASIN":
					result = Math.Asin(ToRadians(dnumA));
					break;

				case "ATAN":
					result = Math.Atan(ToRadians(dnumA));
					break;

				case "LOG": // Log w/ base 10
					result = Math.Log10(dnumA);
					break;

				case "MATH_LN": // Natural logarithm (e as base)
					result = Math.Log(dnumA);
					break;

				case "EPOW":
					result = Math.Exp(dnumA);
					break;

				case "TENPOW":
					result = Math.Pow(10, dnumA);
					break;

				case "TORAD":
					result = ToRadians(dnumA);
					break;

				case "TODEG":
					result = ToDegrees(dnumA);
					break;

				case "FLR":
					result = Math.Floor(dnumA);
					break;

				case "CEIL":
					result = Math.Ceiling(dnumA);
					break;

				default:
					result = 0.0d;
					SendMessage(Level.ERR, $"Unrecognized function {function}.");
					break;
			}
			string resultS = result.ToString();
			SetVariable(destination, ref resultS);
		}

		// PerformOp(): Performs an operation with two values given.
		private void PerformOp(string operation, string varName, string num1, string num2)
		{
			double numberA, numberB;
			if (num2 == null)
			{
				string num1_var = '$' + varName;
				LocalMemoryGet(ref num1_var);
				if (!double.TryParse(num1_var, out numberA)) { SendMessage(Level.ERR, "Malformed number found."); }
				if (!double.TryParse(num1, out numberB)) { SendMessage(Level.ERR, "Malformed number found."); }
			}
			else
			{
				if (!double.TryParse(num1, out numberA)) { SendMessage(Level.ERR, "Malformed number found."); }
				if (!double.TryParse(num2, out numberB)) { SendMessage(Level.ERR, "Malformed number found."); }
			}
			string result = "";
			switch (operation)
			{
				case "min":
					result = Math.Min(numberA, numberB).ToString();
					break;

				case "max":
					result = Math.Max(numberA, numberB).ToString();
					break;

				default:
					SendMessage(Level.ERR, $"Unrecognized operation {operation}.");
					break;
			}
			SetVariable(varName, ref result);
		}

		// Compares two values inside the args array, and stores the result in compareOutput.
		internal bool Compare(ref string[] args)
		{
			bool r; // Output variable (result)
			bool b1, b2;
			// Numbers
			b1 = args[1] == "true" || args[1] == "1";
			b2 = args[2] == "true" || args[2] == "1";
			if (!double.TryParse(args[1], out double n1))
			{
				n1 = 0.0d;
			}
			if (!double.TryParse(args[2], out double n2))
			{
				n2 = 0.0d;
			}

			switch (args[0].ToUpper())
			{
				case "EQL":
				case "E":
					r = args[1] == args[2];
					break;

				case "NEQL":
				case "NE":
					r = args[1] != args[2];
					break;

				case "G":
					r = n1 > n2;
					break;

				case "NG":
					r = !(n1 > n2);
					break;

				case "GE":
					r = n1 >= n2;
					break;

				case "NGE":
					r = !(n1 >= n2);
					break;

				case "L":
					r = n1 < n2;
					break;

				case "NL":
					r = !(n1 < n2);
					break;

				case "LE":
					r = n1 <= n2;
					break;

				case "NLE":
					r = !(n1 <= n2);
					break;

				case "OR":
					r = b1 || b2;
					break;

				case "AND":
					r = b1 && b2;
					break;

				case "XOR":
					r = (b1 || b2) && !(b1 && b2);
					break;

				case "XNOR":
					r = !((b1 || b2) && !(b1 && b2));
					break;

				case "NOR":
					r = (b1 == false) && b2 == false;
					break;

				case "NAND":
					r = !(b1 && b2);
					break;

				default:
					r = false;
					SendMessage(Level.ERR, $"Unrecognized CMPR option {args[0].ToUpper()}.");
					break;

			}
			return r;
		}

		// MathOperation(): Calculator
		private double MathOperation(char op, string destination, string number, string optnumber = null)
		{
			double num1, num2;
			if (optnumber == null)
			{
				string tmp1 = "$" + destination;
				LocalMemoryGet(ref tmp1);
				if (!double.TryParse(tmp1, out num1))
				{
					SendMessage(Level.ERR, "Malformed number found.");
				}
				if (!double.TryParse(number, out num2))
				{
					SendMessage(Level.ERR, "Malformed number found.");
				}
			}
			else
			{
				if (!double.TryParse(number, out num1))
				{
					SendMessage(Level.ERR, "Malformed number found.");
				}
				if (!double.TryParse(optnumber, out num2))
				{
					SendMessage(Level.ERR, "Malformed number found.");
				}
			}

			switch (op)
			{
				case '+':
					return num1 + num2;
				case '-':
					return num1 - num2;
				case '*':
					return num1 * num2;
				case '/':
					return num1 / num2;
				case '%':
					return num1 % num2;
				default:
					SendMessage(Level.ERR, $"Invalid operator {op} used");
					return 0.0d;
			}
		}

		// SetVariable(): Sets the variable with the name varName to newData. Lets the user know if it doesn't exist.
		internal void SetVariable(string varName, ref string newData)
		{
			if (localMemory.ContainsKey(varName))
			{
				localMemory[varName] = newData;
			}
			else
			{
				SendMessage(Level.ERR, $"The variable {varName} does not exist.");
			}
		}

		// LocalMemoryGet(): Converts a given variable to its contents. Leaves it alone if it doesn't have a recognized prefix.
		internal void LocalMemoryGet(ref string varName)
		{
			if (varName.Length == 0)
			{
				varName = "NULL";
				SendMessage(Level.ERR, "Malformed variable");
				return;
			}
			if (varName[0] == '$')
			{
				if (varName[1] == '_')
				{
					if (predefinedVariables.ContainsKey(varName[2..]))
					{
						varName = (string)predefinedVariables[varName[2..]].DynamicInvoke();
					}
				}
				else if (localMemory.ContainsKey(varName[1..]))
				{
					varName = localMemory[varName[1..]];
				}
				else
				{
					SendMessage(Level.ERR, $"The variable {varName[1..]} does not exist.");
					varName = "NULL";
				}
			}
			else if (Program.applicationMode == Mode.EXTENDED)
			{
				if (varName[0] == '#') // Get memory address e.g. where A is the memory address: #A
				{
					if (!int.TryParse(varName[1..], out int address)) { address = -1; SendMessage(Level.ERR, "Malformed memory address found."); }
					if (Program.memory.Exists(address))
					{
						Program.memory.Get(address, out varName);
					}
					else
					{
						SendMessage(Level.ERR, $"Tried accessing unallocated memory space {address}.");
						varName = "NULL";
					}
				}
				else if (varName[0] == '%') // Get char at index A of string B: %A,B
				{
					if (varName.Contains('.'))
					{
						string variable = varName[(varName.IndexOf('.') + 1)..];
						string index = varName[..varName.IndexOf('.')][1..];
						LocalMemoryGet(ref variable);
						LocalMemoryGet(ref index);

						if (!int.TryParse(index, out int index_int)) { SendMessage(Level.ERR, "Malformed number found as index parameter."); }

						else
						{
							varName = variable[index_int].ToString();
						}
					}
					else
					{
						SendMessage(Level.ERR, $"Corrupted variable name syntax {varName} for index.");
						varName = "NULL";
					}
				}
				else if (varName[0] == '!') // Inverse statement e.g. where A is true, it will become false: !A
				{
					string variable = varName[1..];
					LocalMemoryGet(ref variable);
					variable = variable.ToLower();
					bool statement = variable == "true" || variable == "1";
					varName = (!statement).ToString().ToLower();
				}
			}
		}

		// ExtractArgs(): Simply extracts the arguments from array lineInfo, treating quote blocks as one.
		internal string[] ExtractArgs(ref string[] lineInfo)
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
				if (combined[i] == '\\')
				{
					if (combined[i - 1] == '\\' && combined[i - 2] != '\\')
					{
						continue;
					}
				}
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
							newCombined = newCombined[..(newCombined.Length - 1)] + '"'; // Exclude the last/escape character AND include quote
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
					newCombinedList[i] = tmp;
				}
			}

			return newCombinedList.ToArray();
		}

		// ExtractEngineArgs(): Extracts [A B] like stuff and applies it to internal engine variables.
		private void ExtractEngineArgs(ref string[] lineInfo)
		{
			string[] engineArgParts;
			string engineArg = "";
			foreach (string part in lineInfo)
			{
				engineArg += ' ' + part;
			}
			engineArgParts = engineArg[1..].Trim('[', ']').Split(' ');
			if (engineArgParts[0].ToLower() == "mode")
			{
				switch (engineArgParts[1].ToLower())
				{
					case "vsb":
						if (Program.applicationMode != Mode.VSB)
						{
							Program.extendedMode.Dispose(); // Destruct extended mode, thus freeing up memory
							Program.applicationMode = Mode.VSB;
							SendMessage(Level.INF, "Using compat mode");

							SomethingHappened();
						}
						break;

					case "extended":
						if (Program.applicationMode != Mode.EXTENDED)
						{
							Program.extendedMode = new ExtendedInstructions();
							Program.applicationMode = Mode.EXTENDED;
							SendMessage(Level.INF, "Using extended mode");

							bool isAvailable = false;
							for (int attempts = 1; attempts <= 10; attempts++)
							{
								lock (Program.internalShared.SyncRoot)
								{
									isAvailable = Program.internalShared[3] == "TRUE";
								}
								if (!isAvailable)
								{
									SendMessage(Level.WRN, "Screen component is unavailable. Attempt nr. " + attempts);
									Thread.Sleep(80);
								}
								else
								{
									break;
								}
							}
							if (!isAvailable)
							{
								SendMessage(Level.ERR, "Screen component is unavailable. Failed.");
							}
							else
							{
								lock (Program.internalShared.SyncRoot)
								{
									Program.internalShared[2] = "TRUE";
								}
								Program.windowProcess.Interrupt();
								Thread.Sleep(160); //FIXME This should not stay like this due to inconsistent delay it causes, maybe lock check again for "INTOK".
							}
						}
						break;

					default:
						SendMessage(Level.ERR, "Unrecognized mode entered.");
						break;
				}
			}
		}
	}
}
