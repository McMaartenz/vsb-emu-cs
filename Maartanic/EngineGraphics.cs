using System.Drawing;

namespace Maartanic
{
	public class EngineGraphics
	{
		private Pen internalPen;
		public EngineGraphics()
		{
			internalPen = new Pen(Color.White, 1.0f);
		}

		internal void SetColor(Color color)
		{
			internalPen.Color = color;
		}

		internal void Line(float x, float y, float x1, float y1)
		{
			OutputForm.windowGraphics.DrawLine(internalPen, x, y, x1, y1);
		}
	}
}