using System.Drawing;

namespace Maartanic
{
	internal class EngineGraphics
	{
		private readonly Pen internalPen;
		private Brush internalBrush;
		private Font font;
		internal EngineGraphics()
		{
			internalPen = new Pen(Color.White, 1.0f);
			font = new Font(FontFamily.GenericMonospace, 8.0F, FontStyle.Regular);
		}

		private void Initialize()
		{
			OutputForm.windowGraphics = OutputForm.app.CreateGraphics();
		}

		private void DisposeGraphics()
		{
			OutputForm.windowGraphics.Dispose();
		}

		internal void SetColor(Color color)
		{
			internalPen.Color = color;
		}

		internal Color GetColor()
		{
			return internalPen.Color;
		}

		internal void Line(float x, float y, float x1, float y1)
		{
			Initialize();
			OutputForm.windowGraphics.DrawLine(internalPen, x, y, x1, y1);
			DisposeGraphics();
		}

		internal void Rectangle(float x, float y, float w, float h)
		{
			Initialize();
			OutputForm.windowGraphics.DrawRectangle(internalPen, x, y, w, h);
			DisposeGraphics();
		}

		internal void Fill(Color color)
		{
			Initialize();
			OutputForm.windowGraphics.Clear(color);
			DisposeGraphics();
		}

		internal void Pixel(float x, float y)
		{
			internalBrush = new SolidBrush(internalPen.Color);
			Initialize();
			OutputForm.windowGraphics.FillRectangle(internalBrush, x, y, 1, 1);
			DisposeGraphics();
			internalBrush.Dispose();
		}

		internal void Write(float x, float y, string text)
		{
			internalBrush = new SolidBrush(internalPen.Color);
			Initialize();
			OutputForm.windowGraphics.DrawString(text, font, internalBrush, new PointF(x, y));
			DisposeGraphics();
			internalBrush.Dispose();
		}
	}
}