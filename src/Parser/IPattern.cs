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
		public int TryParse(TokenStream stream, ref T item);

		bool IPattern<T>.TryParse(ReaderFork<IToken> source, out T item)
		{
			item = default;

			var parsed = TryParse(source.Memory.Span, ref item);

			if (parsed == 0) return false;
			if (parsed < 0) return true;

			source.Advance(parsed);
			return true;
		}
	}
}