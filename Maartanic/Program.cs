﻿using System;
using System.Threading;

namespace Maartanic
{
	public class Program
	{

		//FIXME We keep using tryparse, but we should just make a function out of it.
		//FIXNOW null reference, most instructions require arguments but if none are given it returns a null reference exception.
		//FIXNOW VSB Compatibility layer for graphics using extended mode.
		//NOTICE probably should use events for cross thread communication, instead of checking if a value in a shared stuff is something.

		internal const float VERSION = 0.9f;

		internal static EngineStack stack = new EngineStack();
		internal static EngineQueue queue = new EngineQueue();
		internal static EngineMemory memory = new EngineMemory();
		internal static EngineGraphics graphics = new EngineGraphics();

		internal static string[] internalShared = new string[4]
		{
			"TRUE",		// isRunning? Threads should close when this is "FALSE"
			"NULL",		// Reason isRunning is set to false
			"FALSE",	// If window is ready to show
			"FALSE"		// If window process is ready to be interrupted
		};

		internal static ExtendedInstructions extendedMode;
		internal static Engine.Mode applicationMode = Engine.Mode.VSB;

		internal static int WIN_WIDTH = 120;
		internal static int WIN_HEIGHT = 30;

		internal static Thread consoleProcess;
		internal static Thread windowProcess;

		internal static byte logLevel;
		internal static Engine EN;

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
				_ => $"Process closed with value {value}.",
			};
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
			windowProcess = new Thread(formWindowStarter)
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
			EN = new Engine(args[0]);

			if (EN.Executable())
			{
				try
				{
					Exit(EN.StartExecution(logLevel));
				}
				catch (Exception ex)
				{
					Console.WriteLine("INTERNAL MRT ERROR: " + ex.ToString());
				}
			}

			Exit("0");
		}
	}
}
