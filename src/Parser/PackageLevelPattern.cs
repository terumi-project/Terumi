using System.Collections.Generic;

using Terumi.Tokens;

namespace Terumi.Parser
{
	public class PackageLevelPattern : IPattern<PackageLevel>
	{
		public int TryParse(TokenStream stream, ref PackageLevel item)
		{
			if (!stream.NextNoWhitespace<IdentifierToken>(out var identifier)) return 0;

			if (identifier.IdentifierCase != IdentifierCase.SnakeCase)
			{
				Log.Error($"Namespace levels must be in snake_case {stream.TopInfo}");
				return 0;
			}

			var levels = new List<string> { identifier };

			while (stream.NextChar('.'))
			{
				if (!stream.NextNoWhitespace<IdentifierToken>(out identifier)) return 0;

				if (identifier.IdentifierCase != IdentifierCase.SnakeCase)
				{
					Log.Error($"Namespace levels must be in snake_case {stream.TopInfo}");
					return 0;
				}

				levels.Add(identifier);
			}

			item = new PackageLevel(levels);
			return stream;
		}
	}
}