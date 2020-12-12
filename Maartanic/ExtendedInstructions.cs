using System;
using System.Threading;
using System.Collections.Generic;

namespace Maartanic
{
	internal class ExtendedInstructions
	{

		internal bool recognizedInstruction = false;

		private readonly Dictionary<string, Func<Engine, string>> toBeAdded = new Dictionary<string, Func<Engine, string>>()
		{
			{ "pask",       (e) => OutputForm.app.AskInput() }, // ask with gui interface, invoke on windowProcess thread
			{ "maartanic",  (e) => "true" }, // whether or not this is the Maartanic Engine
			{ "istype",		(e) => e.IsType.ToString() }, // Return last istype instruction output
			{ "pconf",      (e) => OutputForm.app.AskConfirmation().ToString() } // ask with gui interface, invoke on windowProcess thread
		};

		internal ExtendedInstructions()
		{
			foreach (KeyValuePair<string, Func<Engine, string>> x in toBeAdded) // extend predefined variables
			{
				Engine.predefinedVariables.Add(x.Key, x.Value);
			}
		}

		public void Dispose() // Garbage collect it when switching to compat (VSB)
		{
			foreach (KeyValuePair<string, Func<Engine, string>> x in toBeAdded)
			{
				Engine.predefinedVariables.Remove(x.Key);
			}
		}
		private static T Parse<T>(string input)
		{
			return Program.Parse<T>(input);
		}

		private bool InternalCompare(ref string[] compareIn, ref string[] lineInfo, ref Engine e)
		{
			string[] args = e.ExtractArgs(ref lineInfo);
			if (args.Length > 3)
			{
				compareIn[1] = args[2];
				compareIn[2] = args[3];
			}
			else
			{
				compareIn[1] = args[1];
				compareIn[2] = args[2];
			}
			return e.Compare(ref compareIn);
		}

		bool TestCase(ref string[] compareIn, ref string[] lineInfo, ref Engine e, bool? testCase = null)
		{
			if (testCase.HasValue)
			{
				// Update values
				string[] args;
				if (e.childProcess != null)
				{
					args = e.childProcess.ExtractArgs(ref lineInfo);
				}
				else
				{
					args = new string[] { testCase.ToString() };
				}
				return Parse<bool>(args[1]);
			}
			return InternalCompare(ref compareIn, ref lineInfo, ref e);
		}

		internal string Instructions(Engine e, ref string[] lineInfo, ref string[] args)
		{
			switch (lineInfo[0].ToUpper())
			{

				case "ENDF":
				case "ENDW":
				case "ENDDW":
					return e.lineIndex.ToString() + "." + e.returnedValue; // Return address to jump to later and the original return value separated by a dot.

				case "FOR": // FOR [script] [amount]	r-r
							// FOR [amount]				r
					if (args.Length > 1)
					{
						int amount = (int)Parse<float>(args[1]);
						if (amount < 1)
						{
							e.SendMessage(Engine.Level.INF, "Invalid FOR");
						}
						else
						{
							bool selfRegulatedBreak = false;
							bool skipNext = false;
							for (int i = 0; i < amount; i++)
							{
								e.childProcess = new Engine(e.scriptFile, args[0]);
								if (e.childProcess.Executable())
								{
									if (!skipNext)
									{
										e.returnedValue = e.childProcess.returnedValue = e.childProcess.StartExecution();
									}
									else
									{
										skipNext = false;
									}
									if (e.childProcess.returnedValue.StartsWith("3&"))
									{
										e.SendMessage(Engine.Level.INF, "Break statement");
										selfRegulatedBreak = true;
										break;
									}
									else if (e.childProcess.returnedValue.StartsWith("4&"))
									{
										e.SendMessage(Engine.Level.INF, "Continue statement");
										skipNext = true;
										continue;
									}
								}
								else
								{
									if (!selfRegulatedBreak)
									{
										e.SendMessage(Engine.Level.ERR, "FOR statement failed to execute.");
									}
									else
									{
										e.SendMessage(Engine.Level.INF, "FOR self regulated loop break");
										e.returnedValue = e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('&') + 1)..];
									}
								}
								e.childProcess = null;
							}
						}
					}
					else
					{
						e.childProcess = new Engine(e.scriptFile)
						{
							localMemory = e.localMemory,            // Copy over local memory, and return
							returnedValue = e.returnedValue
						};
						int amount = (int)Parse<float>(args[0]);
						if (amount < 1)
						{
							e.SendMessage(Engine.Level.INF, "Invalid FOR loop");
							e.StatementJumpOut("ENDF", "FOR");
						}
						else
						{
							bool selfRegulatedBreak = false;

							for (int i = 0; i < amount; i++)
							{
								if (i != 0)
								{
									if (e.childProcess.returnedValue.Contains('.'))
									{
										e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('.') + 1)..];
									}
									else
									{
										if (e.childProcess.returnedValue.StartsWith('3') && e.childProcess.returnedValue[1] == '&')
										{
											e.SendMessage(Engine.Level.INF, "Break statement");
											selfRegulatedBreak = true;
											break;
										}

										if (e.childProcess.returnedValue == "4" && e.childProcess.returnedValue[1] == '&')
										{
											e.SendMessage(Engine.Level.INF, "Continue statement");
											e.childProcess.returnedValue = e.childProcess.StartExecution(true, e.lineIndex);
											continue;
										}

										e.SendMessage(Engine.Level.INF, "Return statement");
										string ret = e.childProcess.returnedValue;
										e.childProcess = null;
										return ret;
									}
								}
								e.childProcess.returnedValue = e.childProcess.StartExecution(true, e.lineIndex);
							}
							e.localMemory = e.childProcess.localMemory; // Copy back
							if (e.childProcess.returnedValue.Contains('.'))
							{
								int jumpLine = Parse<int>(e.childProcess.returnedValue[..e.childProcess.returnedValue.IndexOf('.')]);
								e.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('.') + 1)..];
								e.JumpToLine(ref e.sr, ref e.line, ref e.lineIndex, ref jumpLine);
							}
							else
							{
								if (!selfRegulatedBreak)
								{
									e.SendMessage(Engine.Level.ERR, "FOR statement failed to execute.");
								}
								else
								{
									e.SendMessage(Engine.Level.INF, "FOR self regulated loop break");
									e.returnedValue = e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('&') + 1)..];
								}
								e.StatementJumpOut("ENDF", "FOR");
							}
							e.childProcess = null;
						}
					}
					break;

				case "WHILE":   // WHILE [script] [compare instr] [val 1] [val 2]	r-r-r-r
								// WHILE [compare instr] [val 1] [val 2]			r-r-r		
								// WHILE [script] [case]							r-r
								// WHILE [case]										r
					if (args.Length == 2 || args.Length == 4)
					{
						string[] compareIn = new string[3];
						compareIn[0] = args[1];
						bool selfRegulatedBreak = false;
						bool skipNext = false;
						bool? _case = null;
						if (args.Length == 2)
						{
							_case = Parse<bool>(args[1]);
						}
						while (TestCase(ref compareIn, ref lineInfo, ref e, _case))
						{
							e.childProcess = new Engine(e.scriptFile, args[0]);
							if (e.childProcess.Executable())
							{
								if (!skipNext)
								{
									e.returnedValue = e.childProcess.returnedValue = e.childProcess.StartExecution();
								}
								else
								{
									skipNext = false;
								}
								if (e.childProcess.returnedValue.StartsWith("3&"))
								{
									e.SendMessage(Engine.Level.INF, "Break statement");
									selfRegulatedBreak = true;
									break;
								}
								else if (e.childProcess.returnedValue.StartsWith("4&"))
								{
									e.SendMessage(Engine.Level.INF, "Continue statement");
									skipNext = true;
									continue;
								}
							}
							else
							{
								if (!selfRegulatedBreak)
								{
									e.SendMessage(Engine.Level.WRN, "WHILE statement failed to execute.");
								}
								else
								{
									e.SendMessage(Engine.Level.INF, "WHILE self regulated loop break");
									e.returnedValue = e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('&') + 1)..];
								}
								e.StatementJumpOut("ENDW", "WHILE");
							}
							e.childProcess = null;
						}
						break;

					}
					else
					{
						e.childProcess = new Engine(e.scriptFile)
						{
							localMemory = e.localMemory,            // Copy over local memory, and return
							returnedValue = e.returnedValue
						};

						string[] compareIn = new string[3];
						compareIn[0] = args[0];
						bool selfRegulatedBreak = false;
						int i = 0;

						bool? _case = null;
						if (args.Length == 1)
						{
							_case = Parse<bool>(args[0]);
						}

						while (TestCase(ref compareIn, ref lineInfo, ref e, _case))
						{
							if (i != 0)
							{
								if (e.childProcess.returnedValue.Contains('.'))
								{
									e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('.') + 1)..];
								}
								else
								{
									if (e.childProcess.returnedValue.StartsWith("3&"))
									{
										e.SendMessage(Engine.Level.INF, "Break statement");
										selfRegulatedBreak = true;
										break;
									}

									else if (e.childProcess.returnedValue.StartsWith("4&"))
									{
										e.SendMessage(Engine.Level.INF, "Continue statement");
										e.childProcess.returnedValue = e.childProcess.StartExecution(true, e.lineIndex);
										continue;
									}

									else
									{
										e.SendMessage(Engine.Level.INF, "Return statement");
										string returned = e.childProcess.returnedValue;
										e.childProcess = null;
										return returned;
									}
								}
							}
							else
							{
								i++;
							}
							e.childProcess.returnedValue = e.childProcess.StartExecution(true, e.lineIndex);
						}
						e.localMemory = e.childProcess.localMemory; // Copy back
						if (e.childProcess.returnedValue.Contains('.'))
						{
							int jumpLine = Parse<int>(e.childProcess.returnedValue[..e.childProcess.returnedValue.IndexOf('.')]);
							e.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('.') + 1)..];
							e.JumpToLine(ref e.sr, ref e.line, ref e.lineIndex, ref jumpLine);
						}
						else
						{
							if (!selfRegulatedBreak)
							{
								e.SendMessage(Engine.Level.WRN, "WHILE statement failed to execute.");
							}
							else
							{
								e.SendMessage(Engine.Level.INF, "WHILE self regulated loop break");
								e.returnedValue = e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('&') + 1)..];
							}
							e.StatementJumpOut("ENDW", "WHILE");
						}
						e.childProcess = null;
					}
					break;

				case "DOWHILE": // DOWHILE [script] [compare instr] [val 1] [val 2]		r-r-r-r
								// DOWHILE [compare instr] [val 1] [val 2]				r-r-r
								// DOWHILE [script] [case]								r-r			TODO
								// DOWHILE [case]										r			TODO
					if (args.Length == 4 || args.Length == 2)
					{
						string[] compareIn = new string[3];
						compareIn[0] = args[1];
						bool selfRegulatedBreak = false;
						bool skipNext = false;

						bool? _case = null;
						if (args.Length == 2)
						{
							_case = Parse<bool>(args[1]);
						}

						do
						{
							e.childProcess = new Engine(e.scriptFile, args[0]);
							if (e.childProcess.Executable())
							{
								if (!skipNext)
								{
									e.returnedValue = e.childProcess.returnedValue = e.childProcess.StartExecution();
								}
								else
								{
									skipNext = false;
								}
								if (e.childProcess.returnedValue.StartsWith("3&"))
								{
									e.SendMessage(Engine.Level.INF, "Break statement");
									selfRegulatedBreak = true;
									break;
								}
								else if (e.childProcess.returnedValue.StartsWith("4&"))
								{
									e.SendMessage(Engine.Level.INF, "Continue statement");
									continue;
								}
							}
							else
							{
								if (!selfRegulatedBreak)
								{
									e.SendMessage(Engine.Level.ERR, "WHILE statement failed to execute.");
								}
								else
								{
									e.SendMessage(Engine.Level.INF, "WHILE self regulated loop break");
									e.returnedValue = e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('&') + 1)..];
								}
								e.StatementJumpOut("ENDDW", "DOWHILE");
							}
						}
						while (TestCase(ref compareIn, ref lineInfo, ref e, _case));
						//while (InternalCompare(ref compareIn, ref lineInfo, ref e));
						e.childProcess = null;
					}
					else
					{
						e.childProcess = new Engine(e.scriptFile)
						{
							localMemory = e.localMemory,            // Copy over local memory, and return
							returnedValue = e.returnedValue
						};

						string[] compareIn = new string[3];
						compareIn[0] = args[0];
						bool selfRegulatedBreak = false;
						int i = 0;

						bool? _case = null;
						if (args.Length == 1)
						{
							_case = Parse<bool>(args[0]);
						}

						do
						{
							if (i != 0)
							{
								if (e.childProcess.returnedValue.Contains('.'))
								{
									e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('.') + 1)..];
								}
								else
								{
									if (e.childProcess.returnedValue.StartsWith('3') && e.childProcess.returnedValue[1] == '&')
									{
										e.SendMessage(Engine.Level.INF, "Break statement");
										selfRegulatedBreak = true;
										break;
									}

									if (e.childProcess.returnedValue == "4" && e.childProcess.returnedValue[1] == '&')
									{
										e.SendMessage(Engine.Level.INF, "Continue statement");
										e.childProcess.returnedValue = e.childProcess.StartExecution(true, e.lineIndex);
										continue;
									}

									e.SendMessage(Engine.Level.INF, "Return statement");
									string returned = e.childProcess.returnedValue;
									return returned;
								}
							}
							else
							{
								i++;
							}

							e.childProcess.returnedValue = e.childProcess.StartExecution(true, e.lineIndex);
						}
						while (TestCase(ref compareIn, ref lineInfo, ref e, _case));
						e.localMemory = e.childProcess.localMemory; // Copy back
						if (e.childProcess.returnedValue.Contains('.'))
						{
							int jumpLine = Parse<int>(e.childProcess.returnedValue[..e.childProcess.returnedValue.IndexOf('.')]);
							e.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('.') + 1)..];
							e.JumpToLine(ref e.sr, ref e.line, ref e.lineIndex, ref jumpLine);
						}
						else
						{
							if (!selfRegulatedBreak)
							{
								e.SendMessage(Engine.Level.ERR, "DOWHILE statement failed to execute.");
							}
							else
							{
								e.SendMessage(Engine.Level.INF, "DOWHILE self regulated loop break");
								e.returnedValue = e.childProcess.returnedValue = e.childProcess.returnedValue[(e.childProcess.returnedValue.IndexOf('&') + 1)..];
							}
						}
						e.childProcess = null;
					}
					break;

				case "SLEEP": // SLEEP [time] r
					{
						int mseconds = Parse<int>(args[0]);
						try
						{
							Thread.Sleep(mseconds);
						}
						catch (ThreadInterruptedException)
						{
							e.SendMessage(Engine.Level.WRN, "SLEEP interrupted by an internal thread.");
						}
					}
					break;

				case "CASEU": // CASEU [variable] [input] r-o
					{
						string output;
						if (args.Length > 1)
						{
							output = args[1];
						}
						else
						{
							output = '$' + args[0];
							e.LocalMemoryGet(ref output);
						}
						output = output.ToUpper();
						e.SetVariable(args[0], ref output);
					}
					break;

				case "CASEL": // CASEL [variable] [input] r-o
					{
						string output;
						if (args.Length > 1)
						{
							output = args[1];
						}
						else
						{
							output = '$' + args[0];
							e.LocalMemoryGet(ref output);
						}
						output = output.ToLower();
						e.SetVariable(args[0], ref output);
					}
					break;

				case "BREAK":
					return "3&" + e.returnedValue;

				case "CONTINUE":
					return "4&" + e.returnedValue;

				case "NOP": // NO OPERATION
					break;

				case "TOBIN": // TOBIN [variable] [integer] r-o
					{
						int value;
						if (args.Length > 1)
						{
							value = Parse<int>(args[1]);
						}
						else
						{
							string varName = '$' + args[0];
							e.LocalMemoryGet(ref varName);
							value = Parse<int>(varName);
						}
						string binary = Convert.ToString(value, 2);
						e.SetVariable(args[0], ref binary);
					}
					break;

				case "BINTOINT": // BINTOINT [variable] [integer] r-o
					{
						string value;
						if (args.Length > 1)
						{
							value = args[1];
						}
						else
						{
							value = '$' + args[0];
							e.LocalMemoryGet(ref value);
						}
						value = Convert.ToInt32(value, 2).ToString();
						e.SetVariable(args[0], ref value);
					}
					break;

				case "CONMODE": // CONMODE [hide|show] r
					if (args[0].ToUpper() == "HIDE") // 0: SW_HIDE 5: SW_SHOW
					{
						Program.ShowWindow(Program.GetConsoleWindow(), 0);
					}
					else
					{
						Program.ShowWindow(Program.GetConsoleWindow(), 5);
					}
					break;

				case "ISTYPE": // ISTYPE [type] [input] r-r sets $_istype.
					{
						string type = args[0].ToLower(), input = args[1];
						bool result = false;
						switch (type)
						{
							case "int":
							case "int32":
								result = int.TryParse(input, out _);
								break;

							case "bool":
								result = bool.TryParse(input, out _);
								break;

							case "double":
								result = bool.TryParse(input, out _);
								break;

							case "char":
								result = char.TryParse(input, out _);
								break;

							case "byte":
								result = byte.TryParse(input, out _);
								break;

							case "float":
								result = float.TryParse(input, out _);
								break;

							case "uint":
							case "uint32":
								result = uint.TryParse(input, out _);
								break;
						}
						e.IsType = result;
					}
					break;

				default:
					recognizedInstruction = false;
					return null;

			}
			recognizedInstruction = true;
			return null;
		}
	}
}
