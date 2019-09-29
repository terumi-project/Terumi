using System;
using System.Collections.Generic;

using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
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
			if (!source.TryPeek(out _))
			{
				compilerUnit = new CompilerUnit(Array.Empty<CompilerUnitItem>());
				_astNotificationReceiver.AstCreated(source, compilerUnit);
				return true;
			}

			while (_pattern.TryParse(source, out var item))
			{
				items.Add(item);

				// EOF
				if (!source.TryPeek(out _))
				{
					compilerUnit = new CompilerUnit(items.ToArray());
					_astNotificationReceiver.AstCreated(source, compilerUnit);
					return true;
				}
			}

			// pattern couldn't parse
			compilerUnit = default;
			return false;
		}
	}
}