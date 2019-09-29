using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	public class CompilerUnitItemPattern : IPattern<CompilerUnitItem>
	{
		private readonly IPattern<TypeDefinition> _typedefPattern;

		public CompilerUnitItemPattern
		(
			IPattern<TypeDefinition> typeDefinitionPattern
		)
		{
			_typedefPattern = typeDefinitionPattern;
		}

		public bool TryParse(ReaderFork<Token> source, out CompilerUnitItem item)
		{
			using var fork = source.Fork();

			if (_typedefPattern.TryParse(fork, out var typedef))
			{
				fork.Commit = true;
				item = typedef;
				return true;
			}

			item = default;
			return false;
		}
	}
}