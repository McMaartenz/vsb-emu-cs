using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class TitleBar
{

	[DllImport("user32.dll")]
	static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

	// TitleBar(): Hides the titlebar using PInvoke
	public static void Hide()
	{
		IntPtr windowHandle = Process.GetCurrentProcess().MainWindowHandle;
		const int GWL_STYLE = (-16);
		const UInt32 WS_VISIBLE = 0x10000000;
		SetWindowLong(windowHandle, GWL_STYLE, (WS_VISIBLE));
	}
}
