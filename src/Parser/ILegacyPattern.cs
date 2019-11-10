using Terumi.Tokens;

namespace Terumi.Parser
{
	// Add an adapter in the meantime while converting old stuff to the new parser, so that code can still be compiled while being worked on.
	public interface IPattern<T>
	{
		public int TryParse(TokenStream stream, ref T item);
	}
}