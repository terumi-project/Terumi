using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public class NoPattern<T> : IPattern<T>
	{
		public static NoPattern<T> Instance { get; } = new NoPattern<T>();
		public static IPattern<T> IInstance { get; } = Instance;

		public bool TryParse(ReaderFork<Token> source, out T item)
		{
			item = default;
			return false;
		}
	}
}