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
			if (_typedefPattern.TryParse(source.Fork(), out var typedef))
			{
				item = typedef;
				return true;
			}

			item = default;
			return false;
		}
	}
}