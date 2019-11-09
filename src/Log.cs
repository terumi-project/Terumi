using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
		private static Stopwatch _lifetime = Stopwatch.StartNew();
		private static string _stageName = "NONE";
		private static Stopwatch _stopwatch = Stopwatch.StartNew();

		public static void Info(string message) => DisplayMessage(ConsoleColor.Green, "INFO", message);

		public static void Debug(string message)
		{
#if DEBUG
			DisplayMessage(ConsoleColor.Gray, "DEBUG", message);
#endif
		}

		public static void Warn(string message) => DisplayMessage(ConsoleColor.Yellow, "WARN", message);

		public static void Error
		(
			string message,
#if DEBUG
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string filePath = "",
			[CallerLineNumber] int lineNumber = 0
#endif
		) => DisplayMessage
		(
			ConsoleColor.Red,
			"ERR",
#if DEBUG
			$"in '{memberName}'@'{filePath}' line {lineNumber}: " +
#endif
			message
		);

		public static DisposableLoggerEvent Stage(string stageName, string message)
		{
			_stageName = stageName;
			_stopwatch = null;

			DisplayMessage(ConsoleColor.Cyan, "START", message);

			_stopwatch = Stopwatch.StartNew();
			return default;
		}

		public static void StageEnd()
		{
			_stopwatch.Stop();

			DisplayMessage(ConsoleColor.DarkCyan, "STOP", "");

			_stageName = "NONE";
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

		private static void DisplayStage()
		{
			// get 'now' in seconds
			var seconds = _stopwatch?.ElapsedMilliseconds ?? 0L;

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write('[');

			DisplayTime(_lifetime.ElapsedMilliseconds);

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] [");

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(_stageName);

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] [");

			DisplayTime(seconds);

			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write("] ");
		}

		private static void DisplayTime(long ms)
		{
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write(ms.ToString().PadLeft(5, ' '));
			Console.Write(" ms");
		}
	}
}