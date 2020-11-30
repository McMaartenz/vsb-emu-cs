using System;

namespace Maartanic
{
	public class ExtendedInstructions : IDisposable
	{
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		internal void Instructions(ref string[] lineInfo, ref string[] args)
		{
			
		}
	}
}
