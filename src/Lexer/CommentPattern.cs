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
				return ParseMultilineComment(source, meta, out token);
			}
			else if (singleLine)
			{
				return ParseSinglelineComment(source, meta, out token);
			}
			else
			{
				// either one of the previous had to have been hit by now.
				throw new Exception("Reached unreachable point.");
			}
		}

		private static int ParseSinglelineComment(Span<byte> source, LexerMetadata meta, out IToken token)
		{
			var i = 1;

			while (i < source.Length || source[i] == (byte)'\n')
			{
				i++;
			}

			token = new WhitespaceToken(meta);
			return i;
		}

		private static int ParseMultilineComment(Span<byte> source, LexerMetadata meta, out IToken token)
		{
			// TERUMI DECISION:
			// we want to enable /*/ to work
			// if a multiline comment has no ending */ we don't call it an error.

			var i = 1;

			// make sure i + 1 works so we can always lookahead to a slash
			for (; i + 1 < source.Length; i++)
			{
				if (source[i] == Asterisk
					&& source[i + 1] == Slash)
				{
					token = new WhitespaceToken(meta);
					return i + 1;
				}
			}

			// assume multiline comment spans the entire file.
			Log.Warn($"Mutliline comment spans to end of code file. Begins {meta} Ends {meta.FromConsumed(source.Slice(0, i))}");

			token = new WhitespaceToken(meta);
			return i;
		}
	}
}