using Terumi.SyntaxTree;

namespace Terumi.Parser
{
	public class CompilerUnitItemPattern : INewPattern<CompilerUnitItem>
	{
		private readonly INewPattern<Method> _methodPattern;
		private readonly INewPattern<PackageReference> _packagePattern;

		public CompilerUnitItemPattern
		(
			INewPattern<Method> methodPattern,
			INewPattern<PackageReference> packagePattern
		)
		{
			_methodPattern = methodPattern;
			_packagePattern = packagePattern;
		}

		public int TryParse(TokenStream stream, ref CompilerUnitItem item)
		{
			if (stream.TryParse(_methodPattern, out var method))
			{
				item = method;
			}
			else if (stream.TryParse(_packagePattern, out var packageReference))
			{
				item = packageReference;
			}
			else
			{
				return 0;
			}

			return stream;
		}
	}
}