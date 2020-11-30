using System;

namespace Maartanic
{
	public class ExtendedInstructions : IDisposable
	{
		public void Dispose() // Garbage collect it when switching to compat (VSB)
		{
			GC.SuppressFinalize(this);
		}

		internal void Instructions(Engine e, ref string[] lineInfo, ref string[] args)
		{
			switch (lineInfo[0].ToUpper())
			{
				case "FOR":
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
