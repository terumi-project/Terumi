using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class CoagulationTypeDefinitionPattern : IPattern<TypeDefinition>
	{
		private readonly IPattern<TypeDefinition> _classPattern;
		private readonly IPattern<TypeDefinition> _contractPattern;
		private readonly IPattern<TypeDefinition> _methodPattern;

		public CoagulationTypeDefinitionPattern
		(
			IPattern<TypeDefinition> classPattern,
			IPattern<TypeDefinition> contractPattern,
			IPattern<TypeDefinition> methodPattern
		)
		{
			_classPattern = classPattern;
			_contractPattern = contractPattern;
			_methodPattern = methodPattern;
		}

		public bool TryParse(ReaderFork<Token> source, out TypeDefinition item)
			=> TryParse(source, _classPattern, out item)
			|| TryParse(source, _contractPattern, out item)
			|| TryParse(source, _methodPattern, out item);

		private bool TryParse<TDefinition>
		(
			ReaderFork<Token> source,
			IPattern<TDefinition> pattern,
			out TypeDefinition definition
		)
			where TDefinition : TypeDefinition
		{
			using var fork = source.Fork();

			if (pattern.TryParse(fork, out var newDefinition))
			{
				definition = newDefinition;
				fork.Commit = true;
				return true;
			}

			definition = default;
			return false;
		}
	}
}