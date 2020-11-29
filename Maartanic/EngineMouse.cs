using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class EngineMouse
{
	[DllImport("User32.dll")]

	public static extern bool GetCursorPos(out POINT lpPoint);
	private IntPtr handle;

	/* EngineMouse(): Class constructor */
	public EngineMouse()
	{
		IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
	}

	public POINT GetPosition()
	{
		GetCursorPos(out POINT position);
		return position;
	}
}
