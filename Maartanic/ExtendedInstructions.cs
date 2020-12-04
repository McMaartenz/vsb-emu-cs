using System;
using System.Threading;

namespace Maartanic
{
	public class ExtendedInstructions : IDisposable
	{
		public void Dispose() // Garbage collect it when switching to compat (VSB)
		{
			GC.SuppressFinalize(this);
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

		internal string Instructions(Engine e, ref string[] lineInfo, ref string[] args)
		{
			switch (lineInfo[0].ToUpper())
			{

				case "ENDF":
				case "ENDW":
				case "ENDDW":
					return e.lineIndex.ToString() + "." + e.returnedValue; // Return address to jump to later and the original return value separated by a dot.

				case "FOR": // FOR [script] [amount]	r-r
							// FOR [amount]				r		(+ENDFOR)
					if (args.Length > 1)
					{
						Engine forLoopEngine;
						if (!int.TryParse(args[1], out int amount)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }
						for (int i = 0; i < amount; i++)
						{
							forLoopEngine = new Engine(e.scriptFile, args[0]);
							if (forLoopEngine.Executable())
							{
								e.returnedValue = forLoopEngine.returnedValue = forLoopEngine.StartExecution(Program.logLevel);
							}
							else
							{
								e.SendMessage(Engine.Level.ERR, "Program was not executable.");
								break;
							}
						}
					}
					else
					{
						Engine forEngine = new Engine(e.scriptFile)
						{
							localMemory = e.localMemory,            // Copy over local memory, and return
							returnedValue = e.returnedValue
						};

						if (!int.TryParse(args[0], out int amount)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }

						bool selfRegulatedBreak = false;

						for (int i = 0; i < amount; i++)
						{
							if (i != 0)
							{
								if (forEngine.returnedValue.Contains('.'))
								{
									forEngine.returnedValue = forEngine.returnedValue[(forEngine.returnedValue.IndexOf('.') + 1)..];
								}
								else
								{
									if (forEngine.returnedValue.StartsWith('3') && forEngine.returnedValue[1] == '&')
									{
										e.SendMessage(Engine.Level.INF, "Break statement");
										selfRegulatedBreak = true;
										break;
									}

									if (forEngine.returnedValue == "4" && forEngine.returnedValue[1] == '&')
									{
										e.SendMessage(Engine.Level.INF, "Continue statement");
										forEngine.returnedValue = forEngine.StartExecution(Program.logLevel, true, e.lineIndex);
										continue;
									}

									e.SendMessage(Engine.Level.INF, "Return statement");
									return forEngine.returnedValue;
								}
							}
							forEngine.returnedValue = forEngine.StartExecution(Program.logLevel, true, e.lineIndex);
						}
						e.localMemory = forEngine.localMemory; // Copy back
						if (forEngine.returnedValue.Contains('.'))
						{
							if (!int.TryParse(forEngine.returnedValue[..forEngine.returnedValue.IndexOf('.')], out int jumpLine)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }
							e.returnedValue = forEngine.returnedValue[(forEngine.returnedValue.IndexOf('.') + 1)..];
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
								e.returnedValue = forEngine.returnedValue = forEngine.returnedValue[(forEngine.returnedValue.IndexOf('&') + 1)..];
							}
							e.StatementJumpOut("ENDF", "FOR");
						}
					}
					break;

				case "WHILE":   // WHILE [script] [compare instr] [val 1] [val 2]	r-r-r-r
								// WHILE [compare instr] [val 1] [val 2]			r-r-r		(+ENDW)
					if (args.Length > 3)
					{
						Engine whileLoopEngine;

						string[] compareIn = new string[3];
						compareIn[0] = args[1];
						bool selfRegulatedBreak = false;
						bool skipNext = false;

						while (InternalCompare(ref compareIn, ref lineInfo, ref e))
						{
							whileLoopEngine = new Engine(e.scriptFile, args[0]);
							if (whileLoopEngine.Executable())
							{
								if (!skipNext)
								{
									e.returnedValue = whileLoopEngine.returnedValue = whileLoopEngine.StartExecution(Program.logLevel);
								}
								else
								{
									skipNext = false;
								}
								if (whileLoopEngine.returnedValue.StartsWith("3&"))
								{
									e.SendMessage(Engine.Level.INF, "Break statement");
									selfRegulatedBreak = true;
									break;
								}
								else if (whileLoopEngine.returnedValue.StartsWith("4&"))
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
									e.returnedValue = whileLoopEngine.returnedValue = whileLoopEngine.returnedValue[(whileLoopEngine.returnedValue.IndexOf('&') + 1)..];
								}
								e.StatementJumpOut("ENDW", "WHILE");
							}
						}
						break;

					}
					else
					{
						Engine whileEngine = new Engine(e.scriptFile)
						{
							localMemory = e.localMemory,            // Copy over local memory, and return
							returnedValue = e.returnedValue
						};

						string[] compareIn = new string[3];
						compareIn[0] = args[0];
						bool selfRegulatedBreak = false;
						{
							int i = 0;
							while (InternalCompare(ref compareIn, ref lineInfo, ref e))
							{
								if (i != 0)
								{
									if (whileEngine.returnedValue.Contains('.'))
									{
										whileEngine.returnedValue = whileEngine.returnedValue[(whileEngine.returnedValue.IndexOf('.') + 1)..];
									}
									else
									{
										if (whileEngine.returnedValue.StartsWith("3&"))
										{
											e.SendMessage(Engine.Level.INF, "Break statement");
											selfRegulatedBreak = true;
											break;
										}

										else if (whileEngine.returnedValue.StartsWith("4&"))
										{
											e.SendMessage(Engine.Level.INF, "Continue statement");
											whileEngine.returnedValue = whileEngine.StartExecution(Program.logLevel, true, e.lineIndex);
											continue;
										}

										else
										{
											e.SendMessage(Engine.Level.INF, "Return statement");
											return whileEngine.returnedValue;
										}
									}
								}
								else
								{
									i++;
								}
								whileEngine.returnedValue = whileEngine.StartExecution(Program.logLevel, true, e.lineIndex);
							}
						}
						e.localMemory = whileEngine.localMemory; // Copy back
						if (whileEngine.returnedValue.Contains('.'))
						{
							if (!int.TryParse(whileEngine.returnedValue[..whileEngine.returnedValue.IndexOf('.')], out int jumpLine)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }
							e.returnedValue = whileEngine.returnedValue[(whileEngine.returnedValue.IndexOf('.') + 1)..];
							e.JumpToLine(ref e.sr, ref e.line, ref e.lineIndex, ref jumpLine);
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
								e.returnedValue = whileEngine.returnedValue = whileEngine.returnedValue[(whileEngine.returnedValue.IndexOf('&') + 1)..];
							}
							e.StatementJumpOut("ENDW", "WHILE");
						}
					}
					break;

				case "DOWHILE": // DOWHILE [script] [compare instr] [val 1] [val 2]		r-r-r-r
								// DOWHILE [compare instr] [val 1] [val 2]				r-r-r		(+ENDDW)
					if (args.Length > 3)
					{
						Engine whileLoopEngine;

						string[] compareIn = new string[3];
						compareIn[0] = args[1];
						bool selfRegulatedBreak = false;
						bool skipNext = false;

						do
						{
							whileLoopEngine = new Engine(e.scriptFile, args[0]);
							if (whileLoopEngine.Executable())
							{
								if (!skipNext)
								{
									e.returnedValue = whileLoopEngine.returnedValue = whileLoopEngine.StartExecution(Program.logLevel);
								}
								else
								{
									skipNext = false;
								}
								if (whileLoopEngine.returnedValue.StartsWith("3&"))
								{
									e.SendMessage(Engine.Level.INF, "Break statement");
									selfRegulatedBreak = true;
									break;
								}
								else if (whileLoopEngine.returnedValue.StartsWith("4&"))
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
									e.returnedValue = whileLoopEngine.returnedValue = whileLoopEngine.returnedValue[(whileLoopEngine.returnedValue.IndexOf('&') + 1)..];
								}
								e.StatementJumpOut("ENDDW", "DOWHILE");
							}
						}
						while (InternalCompare(ref compareIn, ref lineInfo, ref e));

					}
					else
					{
						Engine whileEngine = new Engine(e.scriptFile)
						{
							localMemory = e.localMemory,            // Copy over local memory, and return
							returnedValue = e.returnedValue
						};

						string[] compareIn = new string[3];
						compareIn[0] = args[0];
						bool selfRegulatedBreak = false;

						int i = 0;
						do
						{
							if (i != 0)
							{
								if (whileEngine.returnedValue.Contains('.'))
								{
									whileEngine.returnedValue = whileEngine.returnedValue[(whileEngine.returnedValue.IndexOf('.') + 1)..];
								}
								else
								{
									if (whileEngine.returnedValue.StartsWith('3') && whileEngine.returnedValue[1] == '&')
									{
										e.SendMessage(Engine.Level.INF, "Break statement");
										selfRegulatedBreak = true;
										break;
									}

									if (whileEngine.returnedValue == "4" && whileEngine.returnedValue[1] == '&')
									{
										e.SendMessage(Engine.Level.INF, "Continue statement");
										whileEngine.returnedValue = whileEngine.StartExecution(Program.logLevel, true, e.lineIndex);
										continue;
									}

									e.SendMessage(Engine.Level.INF, "Return statement");
									return whileEngine.returnedValue;
								}
							}
							else
							{
								i++;
							}

							whileEngine.returnedValue = whileEngine.StartExecution(Program.logLevel, true, e.lineIndex);
						}
						while (InternalCompare(ref compareIn, ref lineInfo, ref e));
						e.localMemory = whileEngine.localMemory; // Copy back
						if (whileEngine.returnedValue.Contains('.'))
						{
							if (!int.TryParse(whileEngine.returnedValue[..whileEngine.returnedValue.IndexOf('.')], out int jumpLine)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }
							e.returnedValue = whileEngine.returnedValue[(whileEngine.returnedValue.IndexOf('.') + 1)..];
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
								e.returnedValue = whileEngine.returnedValue = whileEngine.returnedValue[(whileEngine.returnedValue.IndexOf('&') + 1)..];
							}
						}
					}
					break;

				case "SLEEP": // SLEEP [time] r
					{
						if (!int.TryParse(args[0], out int mseconds)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); break; }
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

				case "SCREENLN": // VSB compat
				case "PLINE": // PLINE [x] [y] [x 1] [y 1] r-r-r-r
					{
						if (!float.TryParse(args[0], out float x)) { e.SendMessage(Engine.Level.ERR, "Malformed floating point number found."); }
						if (!float.TryParse(args[1], out float y)) { e.SendMessage(Engine.Level.ERR, "Malformed floating point number found."); }
						if (!float.TryParse(args[2], out float x1)) { e.SendMessage(Engine.Level.ERR, "Malformed floating point number found."); }
						if (!float.TryParse(args[3], out float y1)) { e.SendMessage(Engine.Level.ERR, "Malformed floating point number found."); }

						Program.graphics.Line(x, y, x1, y1);
					}
					break;

				case "PCOL": // PCOL [Color] r
					{
						string color = args[0];
						color = color[0] == '#' ? color : '#' + color;
						try
						{
							Program.graphics.SetColor(System.Drawing.ColorTranslator.FromHtml(color));// System.Drawing.Color.Red); //System.Drawing.Color.FromArgb(argb)
						}
						catch (ArgumentException)
						{
							e.SendMessage(Engine.Level.ERR, "Malformed hexadecimal number found.");
						}
					}
					break;

				case "SCREENREC": // VSB compat
				case "PRECT": // PRECT [x] [y] [w] [h] r-r-r-r
					{
						if (!float.TryParse(args[0], out float x)) { e.SendMessage(Engine.Level.ERR, "Malformed floating point number found."); }
						if (!float.TryParse(args[1], out float y)) { e.SendMessage(Engine.Level.ERR, "Malformed floating point number found."); }
						if (!float.TryParse(args[2], out float w)) { e.SendMessage(Engine.Level.ERR, "Malformed floating point number found."); }
						if (!float.TryParse(args[3], out float h)) { e.SendMessage(Engine.Level.ERR, "Malformed floating point number found."); }

						if (lineInfo[0].ToUpper() == "SCREENREC")
						{
							w -= x;
							h -= y;
						}

						Program.graphics.Rectangle(x, y, w, h);
					}
					break;

				case "SCREENFILL": // VSB compat
				case "PFILL": // PFILL [color] r
					{
						string color = args[0]; //FIXNOW make function for hex, including the try catch
						color = color[0] == '#' ? color : '#' + color;
						try
						{
							Program.graphics.Fill(System.Drawing.ColorTranslator.FromHtml(color));// System.Drawing.Color.Red); //System.Drawing.Color.FromArgb(argb)
						}
						catch (ArgumentException)
						{
							e.SendMessage(Engine.Level.ERR, "Malformed hexadecimal number found.");
						}

					}
					break;

				case "BREAK":
					return "3&" + e.returnedValue;

				case "CONTINUE":
					return "4&" + e.returnedValue;

				default:
					e.SendMessage(Engine.Level.ERR, $"Unrecognized instruction \"{lineInfo[0]}\". (EXT.)");
					break;

			}
			return null;
		}
	}
}
