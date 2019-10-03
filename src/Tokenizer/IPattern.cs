using Terumi.Tokens;

namespace Terumi.Parser
{
	public interface IPattern<T>
	{
		public bool TryParse(ReaderFork<Token> source, out T item);
	}
}