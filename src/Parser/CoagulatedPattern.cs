using Terumi.Tokens;

namespace Terumi.Parser
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

		public bool TryParse(ReaderFork<IToken> source, out T3 item)
		{
			using var fork1 = source.Fork();
			using var fork2 = source.Fork();

			if (_pattern1.TryParse(fork1, out var t1))
			{
				fork1.Commit = true;
				item = t1;
				return true;
			}
			else if (_pattern2.TryParse(fork2, out var t2))
			{
				fork2.Commit = true;
				item = t2;
				return true;
			}

			item = default;
			return false;
		}
	}
}