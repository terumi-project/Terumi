using System;
using System.Collections.Generic;
using System.IO;

namespace Terumi.Lexer
{
	public class ReaderHead
	{
		private readonly BinaryReader _reader;
		private readonly List<byte> _buffer;
		private int _position;

		public ReaderHead(BinaryReader reader)
		{
			_reader = reader;
			_buffer = new List<byte>();
			_position = 0;
		}

		public int Position => _position;

		public ReaderFork Fork()
		{
			return new ReaderFork
			(
				_position,
				(pos) =>
				{
					var positionOffset = pos - _position;
					var inBuffer = positionOffset < _buffer.Count;

					if (!inBuffer)
					{
						var needToRead = positionOffset - _buffer.Count + 1;
						var bytes = _reader.ReadBytes(needToRead);

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

	public class ReaderFork : IDisposable
	{
		private int _position;
		private readonly int _initPos;
		private readonly Func<int, (bool, byte)> _next;
		private readonly Action<int> _commit;

		public ReaderFork(int position, Func<int, (bool, byte)> next, Action<int> commit)
		{
			_initPos = position;
			_position = position;
			_next = next;
			_commit = commit;
		}

		public int Position => _position;

		public bool TryNext(out byte value)
		{
			var (next, valueDeconstructed) = _next(_position++);
			value = valueDeconstructed;
			return next;
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