using System.Drawing;

namespace Maartanic
{
	internal class EngineGraphics
	{
		private readonly Pen internalPen;
		private Brush internalBrush;
		private readonly Font font;
		private Graphics localGraphics;
		private Bitmap localBitmap = new Bitmap(Program.WIN_WIDTH, Program.WIN_HEIGHT);
		
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

		internal void UpdateInternals(int w, int h)
		{
			localBitmap = new Bitmap(w, h);
			localGraphics = Graphics.FromImage(localBitmap);
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

		internal void Ellipse(float x, float y, float w, float h)
		{
			localGraphics.DrawEllipse(internalPen, x, y, w, h);
		}

		internal void FilledEllipse(float x, float y, float w, float h)
		{
			using Brush internalBrush = new SolidBrush(internalPen.Color);
			localGraphics.FillEllipse(internalBrush, x, y, w, h);
		}

		internal void Pixel(float x, float y)
		{
			using Brush internalBrush = new SolidBrush(internalPen.Color);
			localGraphics.FillRectangle(internalBrush, x, y, 1, 1);
			internalBrush.Dispose();
		}

		internal void Curve(PointF[] points)
		{
			localGraphics.DrawCurve(internalPen, points);
		}

		internal void ClosedCurve(PointF[] points)
		{
			localGraphics.DrawClosedCurve(internalPen, points);
		}

		internal void FilledClosedCurve(PointF[] points)
		{
			using Brush internalBrush = new SolidBrush(internalPen.Color);
			localGraphics.FillClosedCurve(internalBrush, points);
		}

		internal void Bezier(PointF[] points)
		{
			localGraphics.DrawBezier(internalPen, points[0], points[1], points[2], points[3]);
		}

		internal void Beziers(PointF[] points)
		{
			localGraphics.DrawBeziers(internalPen, points);
		}

		internal void Write(float x, float y, string text)
		{
			using Brush internalBrush = new SolidBrush(internalPen.Color);
			localGraphics.DrawString(text, font, internalBrush, new PointF(x, y));
			internalBrush.Dispose();
		}
	}
}