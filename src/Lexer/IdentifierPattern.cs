using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class IdentifierPattern : IPattern
	{
		private readonly IdentifierCase _case;

		public IdentifierPattern(IdentifierCase @case)
		{
			_case = @case;
		}

		public bool TryParse(ReaderFork<byte> source, out Token token)
		{
			Func<char, bool> predicate = IsSnakeCase;

			if (_case == IdentifierCase.PascalCase)
			{
				predicate = IsPascalCase;
			}

			var chars = new List<char>();

			while (source.TryNext(out var currentByte))
			{
				var @char = (char)currentByte;

				if (!predicate(@char))
				{
					break;
				}

				chars.Add(@char);
			}

			if (chars.Count == 0)
			{
				token = default;
				return false;
			}

			source.Back(1);

			token = new IdentifierToken(new string(chars.ToArray()), _case);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static bool IsSnakeCase(char chr)
		{
			return (chr >= 'a' && chr <= 'z')
				|| chr == '_';
		}

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		private static bool IsPascalCase(char chr)
		{
			return (chr >= 'a' && chr <= 'z')
				|| (chr >= 'A' && chr <= 'Z');
		}
	}
}
