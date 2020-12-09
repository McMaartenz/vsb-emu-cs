using System;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Maartanic
{
	public class Program
	{
		//BUG null reference, most instructions require arguments but if none are given it returns a null reference exception. Or you as programmer should just know what you are doing.  Your fault if it crashes.
		//BUG VSB Compatibility layer for graphics using extended mode.
		//IDEA probably should use events for cross thread communication, instead of checking if a value in a shared stuff is something.
		//TODO Add single value for WHILE, FOR, DOWHILE: Just entering TRUE or FALSE. + Support for method true/false instead of compare instruction.
		//TODO Add try catch! And make errors stop the program IF inside extended mode.

		internal const float VERSION = 1.1f;

		internal static EngineStack stack = new EngineStack();
		internal static EngineQueue queue = new EngineQueue();
		internal static EngineMemory memory = new EngineMemory();
		internal static EngineGraphics graphics = new EngineGraphics();

		internal static string[] internalShared = new string[5]
		{
			"TRUE",		// isRunning? Threads should close when this is "FALSE"
			"NULL",		// Reason isRunning is set to false
			"FALSE",	// If window is ready to show
			"FALSE",	// If window process is ready to be interrupted
			"FALSE"			// If EN is initialized properly
		};

		internal static ExtendedInstructions extendedMode;
		internal static Engine.Mode SettingExtendedMode = Engine.Mode.DISABLED;
		internal static Engine.Mode SettingGraphicsMode = Engine.Mode.DISABLED;

		internal static int CON_WIDTH = 120;
		internal static int CON_HEIGHT = 30;

		internal static int WIN_WIDTH = 480;
		internal static int WIN_HEIGHT = 360;

		internal static Thread consoleProcess;
		internal static Thread windowProcess;

		internal static byte logLevel;
		internal static Engine EN;

		internal static bool stopAsking = false;


		// P/Invoke
		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll")]
		internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		internal static extern int GetAsyncKeyState(int vKeys);

		// Exit(): Exit process
		internal static void Exit(string value)
		{
			string R = value switch
			{
				"-1" => "Process closed incorrectly. (code -1)",
				"0" => "Process sucessfully closed. (code 0)",
				"1" => "Process closed due to an internal thread. (code 1)",
				"2" => "Process was manually halted. (code 2)",
				"3" => "Process was closed due to a break statement (code 3)",
				"4" => "Process was closed due to a continue statement (code 4)",
				"5" => "Process succesfully closed. (RET) (code 5)",
				_ => $"Process closed with value {value}.",
			};
			Console.Write('\n' + R);
			Console.ReadLine();
			Environment.Exit(0);
		}

		internal static T Parse<T> (string input, bool silence = false)
		{
			try
			{
				return (T)typeof(T).GetMethod("Parse", new[] { typeof(string) }).Invoke(null, new string[] { input });
			}
			catch (TargetInvocationException)
			{
				if (!silence)
				{
					if (EN != null)
					{
						EN.SendMessage(Engine.Level.ERR, $"Malformed {typeof(T).Name} '{input}' found.");
					}
					else
					{
						Console.Write($"\nINTERNAL MRT ERROR: Malformed {typeof(T).Name} '{input}' found.");
					}
				}
				return default;
			}
			catch (Exception ex)
			{
				Console.Write($"\nINTERNAL MRT ERROR: " + ex);
				return default;
			}
		}

		internal static Color HexHTML (string input)
		{
			if (input.StartsWith("0x"))
			{
				input = input[2..];
			}
			input = (input[0] == '#' ? input : '#' + input).Trim();
			try
			{
				return ColorTranslator.FromHtml(input);
			}
			catch (ArgumentException)
			{
				EN.SendMessage(Engine.Level.ERR, $"Malformed hexadecimal '{input[1..]}' found.");
				return default;
			}
		}

		// Main(): Entry point
		public static void Main(string[] args)
		{			
			consoleProcess = Thread.CurrentThread; // Current thread
			consoleProcess.Name = "consoleProcess";

			ThreadStart formWindowStarter = new ThreadStart(OutputForm.Main); // Window thread
			windowProcess = new Thread(formWindowStarter)
			{
				Name = "windowProcess"
			};
			windowProcess.Start();

			Console.SetBufferSize(CON_WIDTH, CON_HEIGHT); // Remove scrollbar
			Console.SetWindowSize(CON_WIDTH, CON_HEIGHT);

			Console.Title = $"Maartanic Engine {VERSION}";

			Console.WriteLine("Maartanic Engine {0} (partial-gui VSB Engine Emulator on C#)\n", VERSION);
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: mrt [..file]\n"
								+ "Run autorun.mrt [y/N]");
				char ans = Console.ReadLine()[0];
				if (ans != 'y')
				{
					Exit("0");
				}
				args = new string[] { "autorun.mrt" };
			}

			Console.WriteLine("Please enter the log level (0: info 1: warning 2: error");
			logLevel = Parse<byte>(Console.ReadLine());
			if (logLevel < 0 || logLevel > 3)
			{
				logLevel = 0;
			}

			// Clear buffer
			Console.Clear();
			EN = new Engine(args[0]);
			EN.FillPredefinedList();
			if (OutputForm.StartWithGraphics())
			{
				EN.EnableGraphics();
			}
			if (EN.Executable())
			{
				string returnVariable = "";
				try
				{
					do
					{
						returnVariable = EN.StartExecution();
					} while (SettingExtendedMode == Engine.Mode.DISABLED && returnVariable != "5");
					EN.sr.Close();
					EN.sr.Dispose();
					Exit(returnVariable);
				}
				catch (Exception ex)
				{
					Console.Write("\nINTERNAL MRT ERROR: " + ex.ToString());
				}
			}
			Exit("0");
		}
	}
}
