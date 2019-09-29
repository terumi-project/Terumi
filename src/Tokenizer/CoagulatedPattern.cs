using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Ast;
using Terumi.Tokens;

namespace Terumi.Tokenizer
{
	// TODO: dunno if this is needed but eh, leaving it entirely generics for luls
	// is only actually used for TerumiMember, field and method
	public class CoagulatedPattern<T1, T2, T3> : IPattern<T3>
		where T1 : T3
		where T2 : T3
	{
		private readonly IPattern<T1> _pattern1;
		private readonly IPattern<T2> _pattern2;

		public CoagulatedPattern
		(
			IPattern<T1> pattern1,
			IPattern<T2> pattern2
		)
		{
			_pattern1 = pattern1;
			_pattern2 = pattern2;
		}

		public bool TryParse(ReaderFork<Token> source, out T3 item)
		{
			if (_pattern1.TryParse(source.Fork(), out var t1))
			{
				item = t1;
				return true;
			}
			else if (_pattern2.TryParse(source.Fork(), out var t2))
			{
				item = t2;
				return true;
			}

			item = default;
			return false;
		}
	}
}
