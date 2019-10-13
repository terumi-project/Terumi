using System;
using System.Collections.Generic;

using Terumi.SyntaxTree;
using Terumi.Tokens;

namespace Terumi.Parser
{
	public class CompilerUnitPattern : IPattern<CompilerUnit>
	{
		private readonly IPattern<CompilerUnitItem> _pattern;
		private readonly IAstNotificationReceiver _astNotificationReceiver;

		public CompilerUnitPattern
		(
			IAstNotificationReceiver astNotificationReceiver,
			IPattern<CompilerUnitItem> pattern
		)
		{
			_pattern = pattern;
			_astNotificationReceiver = astNotificationReceiver;
		}

		public bool TryParse(ReaderFork<Token> source, out CompilerUnit compilerUnit)
		{
			var items = new List<CompilerUnitItem>();

			// EOF
			if (!source.TryPeekNonWhitespace(out _, out _))
			{
				goto NOTHING_NOTEWORTHY;
			}

			while (_pattern.TryParse(source, out var item))
			{
				items.Add(item);

				// EOF
				if (!source.TryPeekNonWhitespace(out _, out var peeked))
				{
					source.Advance(peeked);

					compilerUnit = new CompilerUnit(items.ToArray());
					_astNotificationReceiver.AstCreated(source, compilerUnit);
					return true;
				}
			}

		// maybe we didn't have anything

		NOTHING_NOTEWORTHY:
			compilerUnit = new CompilerUnit(Array.Empty<CompilerUnitItem>());
			_astNotificationReceiver.AstCreated(source, compilerUnit);
			return true;
		}
	}
}