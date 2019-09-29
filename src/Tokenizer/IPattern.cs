namespace Terumi.Tokenizer
{
	public interface IPattern<T>
	{
		public bool TryParse(ReaderFork<Token> source, out T item);
	}
}