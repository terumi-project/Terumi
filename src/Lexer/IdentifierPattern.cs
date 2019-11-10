using System;
using System.Runtime.CompilerServices;
using System.Text;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class IdentifierPattern : IPattern
	{
		private readonly IdentifierCase _case;
		private readonly Func<byte, bool> _predicate;

		public IdentifierPattern(IdentifierCase @case)
		{
			_case = @case;

			_predicate = IsSnakeCase;

			if (_case == IdentifierCase.PascalCase)
			{
				_predicate = IsPascalCase;
			}
		}

		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			var end = 0;

			for (; end < source.Length; end++)
			{
				if (!_predicate(source[end]))
				{
					// we want to end after we stopped touching the invalid char
					break;
				}
			}

			if (end == 0) return 0;

			var bytes = source.Slice(0, end);
			var str = Encoding.UTF8.GetString(bytes);

			token = new IdentifierToken(meta, str, _case);
			return end;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static bool IsSnakeCase(byte chr)
		{
			return (chr >= 'a' && chr <= 'z')
				|| chr == '_';
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static bool IsPascalCase(byte chr)
		{
			return (chr >= 'a' && chr <= 'z')
				|| (chr >= 'A' && chr <= 'Z');
		}
	}
}