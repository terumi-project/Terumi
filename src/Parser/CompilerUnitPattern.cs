using System;
using System.Collections.Generic;

using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class CompilerUnitPattern : INewPattern<CompilerUnit>
	{
		private readonly INewPattern<CompilerUnitItem> _pattern;

		public CompilerUnitPattern(INewPattern<CompilerUnitItem> pattern)
			=> _pattern = pattern;

		public int TryParse(TokenStream stream, ref CompilerUnit item)
		{
			if (!stream.PeekNextNoWhitespace(out _)) return 0;

			var items = new List<CompilerUnitItem>();
			while (stream.TryParse(_pattern, out var compilerUnitItem))
			{
				items.Add(compilerUnitItem);

				if (!stream.PeekNextNoWhitespace(out _))
				{
					item = new CompilerUnit(items);
					return stream;
				}
			}

			Log.Error($"Failed parsing compiler unit {stream.TopInfo}");
			item = new CompilerUnit(items);
			return stream;
		}
	}
}