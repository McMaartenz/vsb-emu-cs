using System;

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

		internal void Instructions(Engine e, ref string[] lineInfo, ref string[] args)
		{
			switch (lineInfo[0].ToUpper())
			{
				case "FOR": // FOR [script] [amount] r-r
					{
						Engine forLoopEngine;
						if (!Int32.TryParse(args[1], out int amount)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }
						for (int i = 0; i < amount; i++)
						{
							forLoopEngine = new Engine(e.scriptFile, args[0]);
							if (forLoopEngine.Executable())
							{
								e.returnedValue = forLoopEngine.StartExecution(Program.logLevel);
							}
							else
							{
								e.SendMessage(Engine.Level.ERR, "Program was not executable.");
								break;
							}
						}
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
		}
	}
}
