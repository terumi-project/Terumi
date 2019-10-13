﻿using System;
using System.Collections.Generic;

namespace Terumi
{
	public class ReaderHead<T>
	{
		private readonly Func<int, T[]> _readAmount;
		private readonly List<T> _buffer;

		public ReaderHead(Func<int, T[]> reader)
		{
			_readAmount = reader;
			_buffer = new List<T>();
			Position = 0;
		}

		public int Position { get; private set; }

		public ReaderFork<T> Fork()
		{
			return new ReaderFork<T>
			(
				Position,
				(pos) =>
				{
					var positionOffset = pos - Position;
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
					var needToRemove = commitPos - Position;

					if (needToRemove > _buffer.Count)
					{
						// TODO: there's an off by one error somewhere, and there's a hacky workaround in SpecificReaderForkExtensions.cs
						// for peeking and stuff (as well as in ReaderFork)
						// so this needs to be fixed
						// Console.WriteLine("off by one error in reader fork");
						needToRemove = _buffer.Count;
					}

					_buffer.RemoveRange(0, needToRemove);
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