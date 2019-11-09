using Terumi.Tokens;

namespace Terumi.Parser
{
	public interface IPattern<T>
	{
		public bool TryParse(ReaderFork<IToken> source, out T item);
	}
}