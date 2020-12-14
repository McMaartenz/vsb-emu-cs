using System;
using System.Threading;
using System.Collections.Generic;

namespace Maartanic
{
	internal class ExtendedInstructions
	{
		internal Stack<string> usingStatementVariables = new Stack<string>();
		internal Stack<int> usingStatementAmountGenerated = new Stack<int>();
		internal int eventScope = 0; // 0: No trycatch, 10: 10 trycatches in program.
		internal bool recognizedInstruction = false;
		private uint exceptionCode = 0;

		private readonly Dictionary<string, Func<Engine, string>> toBeAdded = new Dictionary<string, Func<Engine, string>>()
		{
			{ "pask",       (e) => OutputForm.app.AskInput() }, // ask with gui interface, invoke on windowProcess thread
			{ "maartanic",  (e) => "true" }, // whether or not this is the Maartanic Engine
			{ "istype",     (e) => e.IsType.ToString() }, // Return last istype instruction output
			{ "pconf",      (e) => OutputForm.app.AskConfirmation().ToString() }, // ask with gui interface, invoke on windowProcess thread
			{ "focus",      (e) => Program.IsFocused().ToString() }, // If console/display is focused.
			{ "time",		(e) => (DateTime.UtcNow - e.startTime).TotalSeconds.ToString() }, // $_projtime but shorter
			{ "pw",         (e) => Program.SettingGraphicsMode == Engine.Mode.ENABLED ? Program.WIN_WIDTH.ToString() : Program.CON_WIDTH.ToString() }, // Display window width
			{ "ph",         (e) => Program.SettingGraphicsMode == Engine.Mode.ENABLED ? Program.WIN_HEIGHT.ToString() : Program.CON_HEIGHT.ToString() }, // Display window height
			{ "scrw",       (e) => OutputForm.app.GetScreenResolution().Width.ToString() }, // Get screen resolution width
			{ "scrh",       (e) => OutputForm.app.GetScreenResolution().Height.ToString() }, // Get screen resolution height
			{ "exc",		(e) => Program.extendedMode.exceptionCode.ToString() }
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
				return Parse<bool>(args.Length > 1 ? args[1] : args[0]);
			}
			return InternalCompare(ref compareIn, ref lineInfo, ref e);
		}
		
		// Return true if event succesfully caught, else return false and let the Engine handle the exception.
		internal bool CatchEvent(Engine e, uint code)
		{
			if (eventScope <= 0)
			{
				return false;
			}
			exceptionCode = code;
			e.StatementJumpOut("CATCH", "TRY");
			return true;
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
										e.SendMessage(Engine.Level.ERR, "FOR statement failed to execute.", 15);
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
						e.childProcess.hasInternalAccess = e.hasInternalAccess;
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
									e.SendMessage(Engine.Level.ERR, "FOR statement failed to execute.", 15);
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
						e.childProcess.hasInternalAccess = e.hasInternalAccess;

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
									e.SendMessage(Engine.Level.ERR, "WHILE statement failed to execute.", 15);
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
						e.childProcess.hasInternalAccess = e.hasInternalAccess;

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
								e.SendMessage(Engine.Level.ERR, "DOWHILE statement failed to execute.", 15);
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
					if (args[0].ToUpper() == "HIDE" && e.hasInternalAccess) // 0: SW_HIDE 5: SW_SHOW // REQUIRES REAL MODE
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

				case "REALMODE": // REALMODE [case]
					{
						bool enable = Parse<bool>(args[0]);
						if (enable)
						{
							enable = Program.RequestPermission(e);
						}
						e.SendMessage(Engine.Level.WRN, "Real mode is " + (enable ? "enabled" : "disabled") + '.');
						e.hasInternalAccess = enable;
					}
					break;

				case "CLP": // CLP [text]
					if (e.hasInternalAccess)
					{
						Program.SetClipboard(args[0]);
					}
					else
					{
						e.SendMessage(Engine.Level.ERR, "Cannot set clipboard outside of real mode.", 16);
					}
					break;

				case "USING": // USING [new variable name] [data] // End with ENDU, and the variable will be "disposed".
							  //TODO enable multiple var creation, for NEW as well. Enable support for it in ENDU.
					string[] generated = e.CreateVariables(ref lineInfo);
					foreach (string s in generated)
					{
						usingStatementVariables.Push(s);
					}
					usingStatementAmountGenerated.Push(generated.Length);
					break;

				case "ENDU":
					for (int i = 0; i < usingStatementAmountGenerated.Peek(); i++)
					{
						e.RemoveVariable(usingStatementVariables.Pop());
					}
					usingStatementAmountGenerated.Pop();
					break;

				case "IINS": // IINS [variable] [coordinate x] [coordinate y] [rec x] [rec y] [width] [height]
					{
						float x = Parse<float>(args[1]), y = Parse<float>(args[2]), rx = Parse<float>(args[3]), ry = Parse<float>(args[4]), rw = Parse<float>(args[5]), rh = Parse<float>(args[6]);
						string res = (x > rx && x < rx + rw && y > ry && y < ry + rh).ToString();
						e.SetVariable(args[0], ref res);
					}
					break;

				case "IINSI": // IINSI [variable] [coordinate x] [coordinate y] [rec x] [rec y] [width] [height]
					{
						float x = Parse<float>(args[1]), y = Parse<float>(args[2]), rx = Parse<float>(args[3]), ry = Parse<float>(args[4]), rw = Parse<float>(args[5]), rh = Parse<float>(args[6]);
						string res = (x >= rx && x <= rx + rw && y >= ry && y <= ry + rh).ToString();
						e.SetVariable(args[0], ref res);
					}
					break;

				case "ININS": // ININS [variable] [coordinate x] [coordinate y] [rec x] [rec y] [width] [height]
					{
						float x = Parse<float>(args[1]), y = Parse<float>(args[2]), rx = Parse<float>(args[3]), ry = Parse<float>(args[4]), rw = Parse<float>(args[5]), rh = Parse<float>(args[6]);
						string res = (!(x > rx && x < rx + rw && y > ry && y < ry + rh)).ToString();
						e.SetVariable(args[0], ref res);
					}
					break;

				case "ININSI": // ININSI [variable] [coordinate x] [coordinate y] [rec x] [rec y] [width] [height]
					{
						float x = Parse<float>(args[1]), y = Parse<float>(args[2]), rx = Parse<float>(args[3]), ry = Parse<float>(args[4]), rw = Parse<float>(args[5]), rh = Parse<float>(args[6]);
						string res = (!(x >= rx && x <= rx + rw && y >= ry && y <= ry + rh)).ToString();
						e.SetVariable(args[0], ref res);
					}
					break;

				case "RECTCH": //RECTCH [variable] [x1] [y1] [w1] [h1] [x2] [y2] [w2] [h2]
					{
						float x = Parse<float>(args[1]), y = Parse<float>(args[2]), w = Parse<float>(args[3]), h = Parse<float>(args[4]), x1 = Parse<float>(args[5]), y1 = Parse<float>(args[6]), w1 = Parse<float>(args[7]), h1 = Parse<float>(args[8]);
						string res = (x < x1 + w1 && x + w > x1 && y < y1 + h1 && y + h > y1).ToString();
						e.SetVariable(args[0], ref res);
					}
					break;

				case "DIST": // DIST [variable] [x] [y] [x2] [y2]
					{
						float x = Parse<float>(args[1]), y = Parse<float>(args[2]), x2 = Parse<float>(args[3]), y2 = Parse<float>(args[4]);
						string dist = Math.Sqrt(Math.Pow(x2 - x, 2) + Math.Pow(y2 - y, 2)).ToString();
						e.SetVariable(args[0], ref dist);
					}
					break;

				case "MIDPNT": // MIDPNT [variable 1] [variable 2] [x] [y] [x2] [y2]
					{
						float x = Parse<float>(args[2]),
							  y = Parse<float>(args[3]),
							 x2 = Parse<float>(args[4]),
							 y2 = Parse<float>(args[5]);

						string pntX = ((x + x2) / 2).ToString(),
							   pntY = ((y + y2) / 2).ToString();
						e.SetVariable(args[0], ref pntX);
						e.SetVariable(args[1], ref pntY);
					}
					break;

				case "TRY": // TRY { code } CATCH { code } FINALLY { code } ENDT
					eventScope++;
					break;

				case "CATCH":
					e.StatementJumpOut("ENDT", "TRY");
					break;

				case "ENDT":
					eventScope--;
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
