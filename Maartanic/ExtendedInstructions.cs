using System;
using System.IO;
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
							localMemory = e.localMemory,			// Copy over local memory, and return
							returnedValue = e.returnedValue
						};

						if (!int.TryParse(args[0], out int amount)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }

						for (int i = 0; i < amount; i++)
						{
							if (i != 0)
							{
								forEngine.returnedValue = forEngine.returnedValue[(forEngine.returnedValue.IndexOf('.') + 1)..];
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
							e.SendMessage(Engine.Level.ERR, "FOR statement failed to execute."); //TODO make function out of this, it's a mess right now. Put function inside Engine.cs
							int scope = 0;
							bool success = false;
							int forWhileIndex = 0;
							string[] cLineInfo = null;
							StreamReader endifsr = new StreamReader(e.scriptFile);
							while ((e.line = endifsr.ReadLine()) != null)
							{
								forWhileIndex++;
								if (e.LineCheck(ref cLineInfo, ref forWhileIndex))
								{
									continue;
								}
								if (forWhileIndex > e.lineIndex)
								{
									if (cLineInfo[0].ToUpper() == "ENDF" && scope == 0)
									{
										success = true;
										break;
									}
									if (cLineInfo[0].ToUpper() == "FOR")
									{
										scope++;
									}
									if (cLineInfo[0].ToUpper() == "ENDF")
									{
										scope--;
									}
								}

							}
							if (success)
							{
								e.SendMessage(Engine.Level.INF, "Continuing at line " + forWhileIndex);
								for (int i = e.lineIndex; i < forWhileIndex; i++)
								{
									if ((e.line = e.sr.ReadLine()) == null)
									{
										break; // safety protection?
									}
								}
								e.lineIndex = forWhileIndex;
							}
							else
							{
								e.SendMessage(Engine.Level.ERR, "Could not jump to end of FOR.");
							}

						}
					}
					break;

				case "WHILE":	// WHILE [script] [compare instr] [val 1] [val 2]	r-r-r-r
								// WHILE [compare instr] [val 1] [val 2]			r-r-r		(+ENDW)
					if (args.Length > 3)
					{
						Engine whileLoopEngine;

						string[] compareIn = new string[3];
						compareIn[0] = args[1];

						while (InternalCompare(ref compareIn, ref lineInfo, ref e))
						{
							whileLoopEngine = new Engine(e.scriptFile, args[0]);
							if (whileLoopEngine.Executable())
							{	
								e.returnedValue = whileLoopEngine.returnedValue = whileLoopEngine.StartExecution(Program.logLevel);
							}
							else
							{
								e.SendMessage(Engine.Level.ERR, "Program was not executable.");
								int scope = 0;
								bool success = false;
								int whileLineIndex = 0;
								string[] cLineInfo = null;
								StreamReader endifsr = new StreamReader(e.scriptFile);
								while ((e.line = endifsr.ReadLine()) != null)
								{
									whileLineIndex++;
									if (e.LineCheck(ref cLineInfo, ref whileLineIndex))
									{
										continue;
									}
									if (whileLineIndex > e.lineIndex)
									{
										if (cLineInfo[0].ToUpper() == "ENDW" && scope == 0)
										{
											success = true;
											break;
										}
										if (cLineInfo[0].ToUpper() == "WHILE")
										{
											scope++;
										}
										if (cLineInfo[0].ToUpper() == "ENDW")
										{
											scope--;
										}
									}

								}
								if (success)
								{
									e.SendMessage(Engine.Level.INF, "Continuing at line " + whileLineIndex);
									for (int i = e.lineIndex; i < whileLineIndex; i++)
									{
										if ((e.line = e.sr.ReadLine()) == null)
										{
											break; // safety protection?
										}
									}
									e.lineIndex = whileLineIndex;
								}
								else
								{
									e.SendMessage(Engine.Level.ERR, "Could not jump to end of WHILE.");
								}
							}
						}
						break;

					}
					else
					{
						Engine whileEngine = new Engine(e.scriptFile)
						{
							localMemory = e.localMemory,			// Copy over local memory, and return
							returnedValue = e.returnedValue
						};

						string[] compareIn = new string[3];
						compareIn[0] = args[0];
						{
							int i = 0;
							while (InternalCompare(ref compareIn, ref lineInfo, ref e))
							{
								if (i != 0)
								{
									whileEngine.returnedValue = whileEngine.returnedValue[(whileEngine.returnedValue.IndexOf('.') + 1)..];
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
							e.SendMessage(Engine.Level.ERR, "WHILE statement failed to execute.");
							int scope = 0;
							bool success = false;
							int whileLineIndex = 0;
							string[] cLineInfo = null;
							StreamReader endifsr = new StreamReader(e.scriptFile);
							while ((e.line = endifsr.ReadLine()) != null)
							{
								whileLineIndex++;
								if (e.LineCheck(ref cLineInfo, ref whileLineIndex))
								{
									continue;
								}
								if (whileLineIndex > e.lineIndex)
								{
									if (cLineInfo[0].ToUpper() == "ENDW" && scope == 0)
									{
										success = true;
										break;
									}
									if (cLineInfo[0].ToUpper() == "WHILE")
									{
										scope++;
									}
									if (cLineInfo[0].ToUpper() == "ENDW")
									{
										scope--;
									}
								}

							}
							if (success)
							{
								e.SendMessage(Engine.Level.INF, "Continuing at line " + whileLineIndex);
								for (int i = e.lineIndex; i < whileLineIndex; i++)
								{
									if ((e.line = e.sr.ReadLine()) == null)
									{
										break; // safety protection?
									}
								}
								e.lineIndex = whileLineIndex;
							}
							else
							{
								e.SendMessage(Engine.Level.ERR, "Could not jump to end of WHILE.");
							}
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

						do
						{
							whileLoopEngine = new Engine(e.scriptFile, args[0]);
							if (whileLoopEngine.Executable())
							{
								e.returnedValue = whileLoopEngine.returnedValue = whileLoopEngine.StartExecution(Program.logLevel);
							}
							else
							{
								e.SendMessage(Engine.Level.ERR, "Program was not executable.");
								break;
							}
						}
						while (InternalCompare(ref compareIn, ref lineInfo, ref e));

					}
					else
					{
						Engine whileEngine = new Engine(e.scriptFile)
						{
							localMemory = e.localMemory,			// Copy over local memory, and return
							returnedValue = e.returnedValue
						};

						string[] compareIn = new string[3];
						compareIn[0] = args[0];

						int i = 0;
						do
						{
							if (i != 0)
							{
								whileEngine.returnedValue = whileEngine.returnedValue[(whileEngine.returnedValue.IndexOf('.') + 1)..];
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
							e.SendMessage(Engine.Level.ERR, "DOWHILE statement failed to execute.");
						}
					}
					break;

				case "SLEEP":
					{
						if (!int.TryParse(args[0], out int mseconds)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); break; }
						Thread.Sleep(mseconds);
					}
					break;

				default:
					e.SendMessage(Engine.Level.ERR, $"Unrecognized instruction \"{lineInfo[0]}\". (EXT.)");
					break;

			}
			return null;
		}
	}
}
