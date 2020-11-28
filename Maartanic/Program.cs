using System;

namespace Maartanic
{
	class Program
	{
		const float VERSION = 0.2f;

		public static void Exit(string value)
		{
			Console.Write($"\nProcess exited with value \"{value}\".");
			Console.ReadLine();
			Environment.Exit(0);
		}

		public static void Main(string[] args)
		{
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
			if (!Byte.TryParse(Console.ReadLine(), out byte logLevel))
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

			Exit("NULL");
		}
	}
}
