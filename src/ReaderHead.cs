using System;
using System.Collections.Generic;

namespace Terumi
{
	public class ReaderHead<T>
	{
		private readonly Memory<T> _memory;

		public ReaderHead(Memory<T> memory)
		{
			Position = 0;
			_memory = memory;
		}

		public int Position { get; private set; }

		public ReaderFork<T> Fork()
		{
			return new ReaderFork<T>
			(
				Position,
				(pos) =>
				{
					// assume pos >= 0

					if (pos >= _memory.Length) return (false, default);

					return (true, _memory.Span[pos]);
				},
				(commitPos) =>
				{
					Position = commitPos;
				}
			);
		}
	}

	public class ReaderFork<T> : IDisposable
	{
		private readonly int _initPos;
		private readonly Func<int, (bool, T)> _next;
		private readonly Action<int> _commit;

		public ReaderFork(int position, Func<int, (bool, T)> next, Action<int> commit)
		{
			_initPos = position;
			Position = position;
			_next = next;
			_commit = commit;
		}

		public ReaderFork<T> Fork()
			=> new ReaderFork<T>(Position, _next, pos => Position = pos);

		public bool TryPeek(out T value, int ahead = 1)
		{
			var (next, valueDeconstructed) = _next(Position + ahead - 1);
			value = valueDeconstructed;
			return next;
		}

		public int Position { get; private set; }

		public bool TryNext(out T value)
		{
			var (next, valueDeconstructed) = _next(Position++);
			value = valueDeconstructed;
			return next;
		}

		public int Advance(int forward)
		{
			var i = 0;

			for (; i < forward && TryNext(out _); i++) ;

			return i;
		}

		public int Back(int back)
		{
			var newPosition = Math.Max(Position - back, _initPos);
			var wentBack = Position - newPosition;

			Position = newPosition;

			return wentBack;
		}

		public bool Commit { get; set; }

		public void Dispose()
		{
			if (Commit)
			{
				_commit(Position);
			}
		}
	}
}