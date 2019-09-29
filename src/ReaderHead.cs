using System;
using System.Collections.Generic;

namespace Terumi
{
	public class ReaderHead<T>
	{
		private readonly Func<int, T[]> _readAmount;
		private readonly List<T> _buffer;
		private int _position;

		public ReaderHead(Func<int, T[]> reader)
		{
			_readAmount = reader;
			_buffer = new List<T>();
			_position = 0;
		}

		public int Position => _position;

		public ReaderFork<T> Fork()
		{
			return new ReaderFork<T>
			(
				_position,
				(pos) =>
				{
					var positionOffset = pos - _position;
					var inBuffer = positionOffset < _buffer.Count;

					if (!inBuffer)
					{
						var needToRead = positionOffset - _buffer.Count + 1;
						var bytes = _readAmount(needToRead);

						_buffer.AddRange(bytes);

						// if we didn't read the amount we want, we're at the end.
						if (bytes.Length != needToRead)
						{
							return (false, default);
						}
					}

					return (true, _buffer[positionOffset]);
				},
				(commitPos) =>
				{
					var needToRemove = commitPos - _position;
					_buffer.RemoveRange(0, needToRemove);
					_position = commitPos;
				}
			);
		}
	}

	public class ReaderFork<T> : IDisposable
	{
		private int _position;
		private readonly int _initPos;
		private readonly Func<int, (bool, T)> _next;
		private readonly Action<int> _commit;

		public ReaderFork(int position, Func<int, (bool, T)> next, Action<int> commit)
		{
			_initPos = position;
			_position = position;
			_next = next;
			_commit = commit;
		}

		public ReaderFork<T> Fork()
			=> new ReaderFork<T>(_position, _next, _commit);

		public bool TryPeek(out T value, int ahead = 1)
		{
			var (next, valueDeconstructed) = _next(_position + ahead);
			value = valueDeconstructed;
			return next;
		}

		public int Position => _position;

		public bool TryNext(out T value)
		{
			var (next, valueDeconstructed) = _next(_position++);
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
			var newPosition = Math.Max(_position - back, _initPos);
			var wentBack = _position - newPosition;

			_position = newPosition;

			return wentBack;
		}

		public bool Commit { get; set; }

		public void Dispose()
		{
			if (Commit)
			{
				_commit(_position);
			}
		}
	}
}