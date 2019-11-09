using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class CompilerUnitItemPattern : IPattern<CompilerUnitItem>
	{
		private readonly IPattern<CompilerUnitItem> _coagulation;

		public CompilerUnitItemPattern
		(
			IPattern<TypeDefinition> typeDefinitionPattern,
			IPattern<PackageReference> packagePattern
		)
		{
			_coagulation = new CoagulatedPattern<TypeDefinition, PackageReference, CompilerUnitItem>
			(
				typeDefinitionPattern,
				packagePattern
			);
		}

		public bool TryParse(ReaderFork<IToken> source, out CompilerUnitItem item)
			=> _coagulation.TryParse(source, out item);
	}
}