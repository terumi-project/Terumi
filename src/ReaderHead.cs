using System;

namespace Terumi
{
	public sealed class ReaderFork<T> : IDisposable
	{
		private readonly Memory<T> _memory;
		private readonly Action<int>? _commit;

		public Memory<T> Memory => _memory.Slice(Position);

		public ReaderFork(int position, Memory<T> memory, Action<int>? commit)
		{
			Position = position;
			_memory = memory;
			_commit = commit;
		}

		public ReaderFork<T> Fork()
			=> new ReaderFork<T>(Position, _memory, pos => Position = pos);

		public bool TryPeek(out T value, int ahead = 1)
		{
			if (_memory.Length < Position + ahead - 1)
			{
				value = default;
				return false;
			}

			value = _memory.Span[Position + ahead - 1];
			return true;
		}

		public int Position { get; private set; }

		public bool TryNext(out T value)
		{
			var result = TryPeek(out value);
			Position++;
			return result;
		}

		public int Advance(int forward)
		{
			var i = 0;

			for (; i < forward && TryNext(out _); i++) ;

			return i;
		}

		public bool Commit { get; set; }

		public void Dispose()
		{
			if (Commit)
			{
				_commit?.Invoke(Position);
			}
		}
	}
}