using System.Runtime.InteropServices;

public class EngineMouse
{
	[DllImport("User32.dll")] // Contains the GetCursorPos()

	private static extern bool GetCursorPos(out POINT lpPoint);

	public POINT position;

	// GetPosition(): Returns a POINT (x,y int struct) of the current mouse position of Windows. Done using userlib32.dll
	public POINT GetPosition()
	{
		GetCursorPos(out position);
		return position;
	}
}
