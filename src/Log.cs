using System;
using System.Diagnostics;

namespace Terumi
{
	public struct DisposableLoggerEvent : IDisposable
	{
		public void Dispose() => Log.StageEnd();
	}

	// static logger makes code neater, but unit testing harder /shrug
	// don't think we'll be testing anyways lol
	public static class Log
	{
		private static string _stageName = "NONE";
		private static Stopwatch _stopwatch = Stopwatch.StartNew();

		public static void Info(string message) => DisplayMessage(ConsoleColor.Green, "INFO", message);

		public static void Warn(string message) => DisplayMessage(ConsoleColor.Yellow, "WARN", message);

		public static void Error(string message) => DisplayMessage(ConsoleColor.Red, "ERR", message);

		public static DisposableLoggerEvent Stage(string stageName)
		{
			_stageName = stageName;
			_stopwatch = null;

			DisplayMessage(ConsoleColor.Cyan, "START", "");

			_stopwatch = Stopwatch.StartNew();
			return default;
		}

		public static void StageEnd()
		{
			_stopwatch.Stop();

			DisplayMessage(ConsoleColor.DarkCyan, "STOP", "");

			_stageName = "NONE";
		}

		private static void DisplayStage()
		{
			// get 'now' in seconds
			var seconds = _stopwatch?.ElapsedMilliseconds ?? 0L;

#if DISPLAY_TIME
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write('[');

			Console.ForegroundColor = ConsoleColor.Gray;
			var now = DateTime.Now;
			Console.Write(now.ToShortDateString() + " " + now.ToShortTimeString());

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] [");
#else
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write('[');
#endif

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(_stageName);

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] [");

			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write(seconds.ToString().PadLeft(5, '0'));
			Console.Write(" ms");

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] ");
		}

		private static void DisplayMessage(ConsoleColor color, string characterized, string message)
		{
			DisplayStage();

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write('[');

			Console.ForegroundColor = color;
			Console.Write(characterized);

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] ");

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(message);
		}
	}
}
