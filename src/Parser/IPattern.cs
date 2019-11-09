using System;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public interface IPattern<T>
	{
		public bool TryParse(ReaderFork<IToken> source, out T item);
	}

	// Add an adapter in the meantime while converting old stuff to the new parser, so that code can still be compiled while being worked on.
	public interface INewPattern<T> : IPattern<T>
	{
		public int TryParse(Memory<IToken> source, ref T item);

		bool IPattern<T>.TryParse(ReaderFork<IToken> source, out T item)
		{
			item = default;

			var parsed = TryParse(source.Memory, ref item);
			if (parsed == 0) return false;

			source.Advance(parsed);

			return true;
		}
	}
}