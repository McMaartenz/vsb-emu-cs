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

		internal void Rectangle(float x, float y, float w, float h)
		{
			OutputForm.windowGraphics.DrawRectangle(internalPen, x, y, w, h);
		}

		internal void Fill(Color color)
		{
			OutputForm.windowGraphics.Clear(color);
		}
	}
}