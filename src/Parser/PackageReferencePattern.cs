using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class PackageReferencePattern : IPattern<PackageReference>
	{
		private readonly IPattern<PackageLevel> _packageLevelPattern;

		public PackageReferencePattern(IPattern<PackageLevel> packageLevelPattern)
			=> _packageLevelPattern = packageLevelPattern;

		public int TryParse(TokenStream stream, ref PackageReference item)
		{
			if (!stream.NextNoWhitespace<KeywordToken>(out var keywordToken)) return 0;

			if (keywordToken.Keyword == Keyword.Using
				|| keywordToken.Keyword == Keyword.Namespace)
			{
				if (!stream.TryParse(_packageLevelPattern, out var packageLevel))
				{
					Log.Error($"Expected valid package level {stream.TopInfo}");
					return 0;
				}

				item = new PackageReference(keywordToken.Keyword, packageLevel);
				return stream;
			}

			return 0;
		}
	}
}