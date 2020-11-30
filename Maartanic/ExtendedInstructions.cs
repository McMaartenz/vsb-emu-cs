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
				default:
					e.SendMessage(Engine.Level.ERR, $"Unrecognized instruction \"{lineInfo[0]}\". (EXT.)");
					break;
			}
		}
	}
}
