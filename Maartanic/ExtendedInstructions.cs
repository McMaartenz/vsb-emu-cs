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
			compareIn[2] = args[2];
			compareIn[3] = args[3];
			return e.Compare(ref compareIn);
		}

		internal void Instructions(Engine e, ref string[] lineInfo, ref string[] args)
		{
			switch (lineInfo[0].ToUpper())
			{
				case "FOR": // FOR [script] [amount] r-r
					{
						string entryPoint = args[0];
						Engine forLoopEngine = new Engine(e.scriptFile);
						if (!Int32.TryParse(args[1], out int amount)) { e.SendMessage(Engine.Level.ERR, "Malformed number found."); }
						for (int i = 0; i < amount; i++)
						{
							if (e.Executable())
							{
								e.entryPoint = entryPoint;
								e.StartExecution(Program.logLevel);
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
						string entryPoint = args[0];
						Engine whileLoopEngine = new Engine(e.scriptFile);

						string[] compareIn = new string[4];

						compareIn[0] = "CMPR";
						compareIn[1] = args[1];

						while (InternalCompare(ref compareIn, ref lineInfo, ref e))
						{
							if (e.Executable())
							{
								e.entryPoint = entryPoint;
								e.StartExecution(Program.logLevel);
							}
							else
							{
								e.SendMessage(Engine.Level.ERR, "Program was not executable.");
								break;
							}
						}

					}
					break;

				default:
					e.SendMessage(Engine.Level.ERR, $"Unrecognized instruction \"{lineInfo[0]}\". (EXT.)");
					break;
			}
		}
	}
}
