using System;
using System.Drawing;
using System.Runtime.InteropServices;

public class EngineMouse
{

	[DllImport("kernel32")]
	private static extern IntPtr GetConsoleWindow();

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

	private POINT position;
	private Rectangle window;

	private int consoleWidth;
	private int consoleHeight;
	private double pxPerColumn;
	private double pxPerRow;

	// GetPosition(): Returns a POINT (x,y int struct) of the current mouse position, relative to the console window.
	public POINT GetPosition()
	{
		GetCursorPos(out position);
		GetWindowRect(Handle, ref window);

		position.X -= window.X;
		position.Y -= window.Y;
		position.X = position.X < 0 ? 0 : position.X;
		position.Y = position.Y < 0 ? 0 : position.Y;
		position.X = position.X > window.Width ? window.Width : position.X;
		position.Y = position.Y > window.Height ? window.Height : position.X;

		consoleWidth = Console.WindowWidth;
		consoleHeight = Console.WindowHeight;

		//TODO: Proper ratio calculation (and check if correct)
		pxPerColumn = (double) window.Width / (double) consoleWidth;
		pxPerRow = (double)window.Height / (double) consoleHeight;

		return position;
	}

	private static IntPtr Handle
	{
		get
		{
			return GetConsoleWindow();
		}
	}
}
