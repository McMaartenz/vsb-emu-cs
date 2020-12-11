using System.Drawing;

namespace Maartanic
{
	internal class EngineGraphics
	{
		private readonly Pen internalPen;
		private Brush internalBrush;
		private readonly Font font;
		private readonly Graphics localGraphics;
		private readonly Bitmap localBitmap = new Bitmap(Program.WIN_WIDTH, Program.WIN_HEIGHT);
		internal EngineGraphics()
		{
			//localBitmap = new Bitmap(width: Program.WIN_WIDTH, height: Program.WIN_HEIGHT);
			localGraphics = Graphics.FromImage(localBitmap);
			internalPen = new Pen(Color.White, 1.0f);
			font = new Font(FontFamily.GenericMonospace, 8.0F, FontStyle.Regular);
		}

		internal void Update()
		{
			using (OutputForm.windowGraphics = OutputForm.app.CreateGraphics())
			{
				OutputForm.windowGraphics.DrawImage(localBitmap, 0, 0);
			}
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
			localGraphics.DrawLine(internalPen, x, y, x1, y1);
		}

		internal void Rectangle(float x, float y, float w, float h)
		{
			localGraphics.DrawRectangle(internalPen, x, y, w, h);
		}

		internal void Fill(Color color)
		{
			localGraphics.Clear(color);
		}

		internal void FilledRectangle(float x, float y, float w, float h)
		{
			//localGraphics.DrawRectangle(internalPen, x, y, w, h);
			using Brush internalBrush = new SolidBrush(internalPen.Color);
			localGraphics.FillRectangle(internalBrush, x, y, w, h);
		}

		internal void Pixel(float x, float y)
		{
			internalBrush = new SolidBrush(internalPen.Color);
			localGraphics.FillRectangle(internalBrush, x, y, 1, 1);
			internalBrush.Dispose();
		}

		internal void Write(float x, float y, string text)
		{
			internalBrush = new SolidBrush(internalPen.Color);
			localGraphics.DrawString(text, font, internalBrush, new PointF(x, y));
			internalBrush.Dispose();
		}
	}
}