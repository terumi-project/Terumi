using Terumi.SyntaxTree;

namespace Terumi.Parser
{
	public class CompilerUnitItemPattern : IPattern<CompilerUnitItem>
	{
		private readonly IPattern<Method> _methodPattern;
		private readonly IPattern<PackageReference> _packagePattern;

		public CompilerUnitItemPattern
		(
			IPattern<Method> methodPattern,
			IPattern<PackageReference> packagePattern
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