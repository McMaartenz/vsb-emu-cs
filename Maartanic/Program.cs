using System;
using System.Threading;

namespace Maartanic
{
	public class Program
	{

		public const float VERSION = 0.9f;

		internal static EngineStack stack = new EngineStack();
		internal static EngineQueue queue = new EngineQueue();
		internal static EngineMemory memory = new EngineMemory();
		internal static EngineGraphics graphics = new EngineGraphics();

		internal static string[] internalShared = new string[2]
		{
			"TRUE",		// isRunning? Threads should close when this is "FALSE"
			"NULL"		// Reason isRunning is set to false
		};

		public static ExtendedInstructions extendedMode;
		internal static Engine.Mode applicationMode = Engine.Mode.VSB;

		public static int WIN_WIDTH = 120;
		public static int WIN_HEIGHT = 30;

		internal static Thread consoleProcess;

		internal static byte logLevel;

		// Exit(): Exit process
		public static void Exit(string value)
		{
			string R;
			switch(value)
			{
				case "-1":
					R = "Process closed incorrectly. (code -1)";
					break;

				case "0":
					R = "Process sucessfully closed. (code 0)";
					break;

				case "1":
					R = "Process closed due to an internal thread. (code 1)";
					break;

				case "2":
					R = "Process was manually halted. (code 2)";
					break;

				default:
					R = $"Process closed with value {value}.";
					break;
			}
			Console.Write('\n' + R);
			Console.ReadLine();
			Environment.Exit(0);
		}

		// Main(): Entry point
		public static void Main(string[] args)
		{
			consoleProcess = Thread.CurrentThread; // Current thread
			consoleProcess.Name = "consoleProcess";

			ThreadStart formWindowStarter = new ThreadStart(OutputForm.Main); // Window thread
			Thread windowProcess = new Thread(formWindowStarter)
			{
				Name = "windowProcess"
			};
			windowProcess.Start();

			Console.SetBufferSize(WIN_WIDTH, WIN_HEIGHT); // Remove scrollbar
			Console.SetWindowSize(WIN_WIDTH, WIN_HEIGHT);

			Console.Title = $"Maartanic Engine {VERSION}";

			Console.WriteLine("Maartanic Engine {0} (no-gui VSB Engine Emulator on C#)\n", VERSION);
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: mrt [..file]\n"
								+ "Run autorun.mrt [y/N]");
				char ans = Console.ReadLine()[0];
				if (ans != 'y')
				{
					return;
				}
				args = new string[] { "autorun.mrt" };
			}

			Console.WriteLine("Please enter the log level (0: info 1: warning 2: error");
			if (!byte.TryParse(Console.ReadLine(), out logLevel))
			{
				logLevel = 0;
			}
			if (logLevel < 0 || logLevel > 2)
			{
				logLevel = 0;
			}

			// Clear buffer
			Console.Clear();
			Engine e = new Engine(args[0]);

			if (e.Executable())
			{
				Exit(e.StartExecution(logLevel));
			}

			Exit("0");
		}
	}
}
