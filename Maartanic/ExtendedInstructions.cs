using System;
using System.IO;

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
			compareIn[1] = args[2];
			compareIn[2] = args[3];
			return e.Compare(ref compareIn);
		}

		internal string Instructions(Engine e, ref string[] lineInfo, ref string[] args)
		{
			switch (lineInfo[0].ToUpper())
			{

				case "ENDFOR":
					return e.lineIndex.ToString(); // Return address to jump to later

				case "FOR": // FOR [script] [amount] r-r =OR= FOR [amount] r (+ENDFOR)
					if (args.Length > 1)
					{
						Engine forLoopEngine;
						if (!Int32.TryParse(args[1], out int amount)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }
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
						Engine forEngine = new Engine(e.scriptFile, e.lineIndex);
						forEngine.localMemory = e.localMemory; // Copy over
						forEngine.applicationMode = Engine.Mode.EXTENDED; // Enable extended

						if (!int.TryParse(args[0], out int amount)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }

						for (int i = 0; i < amount; i++)
						{
							forEngine.returnedValue = forEngine.StartExecution(Program.logLevel, true, e.lineIndex);
						}
						e.localMemory = forEngine.localMemory; // Copy back

						if (!int.TryParse(forEngine.returnedValue, out int jumpLine)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }
						e.JumpToLine(ref e.sr, ref e.line, ref e.lineIndex, ref jumpLine);
					}
					break;

				case "WHILE": // WHILE [script] [compare Instr] [val 1] [val 2] r-r-r-r
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
								break;
							}
						}

					}
					break;

				case "DOWHILE": // DOWHILE [script] [compare Instr] [val 1] [val 2] r-r-r-r
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
					break;

				default:
					e.SendMessage(Engine.Level.ERR, $"Unrecognized instruction \"{lineInfo[0]}\". (EXT.)");
					break;

			}
			return null;
		}
	}
}
