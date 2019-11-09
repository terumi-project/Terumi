using System;
using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class CommentPattern : IPattern
	{
		public const byte Slash = (byte)'/';
		public const byte Asterisk = (byte)'*';

		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			if (source.Length < 2) return 0;
			if (source[0] != Slash) return 0;

			var second = source[1];
			var singleLine = second == Slash;
			var multiLine = second == Asterisk;
			if (!singleLine && !multiLine) return 0;

			// comments are unimportant to computers so they're considered whitespace :^)
			// TODO: don't be lazy and make comments not completely die from the AST

			if (multiLine)
			{
				// TERUMI DECISION:
				// we want to enable /*/ to work
				// if a multiline comment has no ending */ we don't call it an error.

				var i = 1;

				// make sure i + 1 works so we can always lookahead to a slash
				for(; i + 1 < source.Length; i++)
				{
					if (source[i] == Asterisk
						&& source[i + 1] == Slash)
					{
						token = new WhitespaceToken(meta);
						return i + 1;
					}
				}

				// assume multiline comment spans the entire file.
				Log.Warn($"Mutliline comment spans to end of code file {meta.ToInfo()}");

				token = new WhitespaceToken(meta);
				return i;
			}
			else if (singleLine)
			{
				var i = 1;

				for(; i < source.Length; i++)
				{
					if (source[i] == (byte)'\n')
					{
						break;
					}
				}

				token = new WhitespaceToken(meta);
				return i;
			}
			else
			{
				throw new Exception("Reached unreachable point.");
			}
		}
	}
}