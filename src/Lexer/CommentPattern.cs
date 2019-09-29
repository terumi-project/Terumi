using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class CommentPattern : IPattern
	{
		public bool TryParse(ReaderFork<byte> source, out Token token)
		{
			int start = source.Position;

			if (!(source.TryNext(out var firstSlash)
				&& firstSlash == (byte)'/'
				&& source.TryNext(out var secondSlash)
				&& (secondSlash == (byte)'/' || secondSlash == (byte)'*')))
			{
				token = default;
				return false;
			}

			// comments are unimportant to computers so they're considered whitespace :^)
			// TODO: don't be lazy and make comments not completely die from the AST

			// multiline?
			if (secondSlash == '*')
			{
				// a common problem of programming languages is that you have to have an ending /*
				// but sometimes i just want to comment out everything, y'know?
				// so if we can't match */ we just assume the comment extends to EOF.

				// another thing i dislike is having to have /**/, i want to be able to do /*/
				// so we support that for god knows why

				bool needsAstriek = false;

				while (source.TryNext(out var current))
				{
					if (!needsAstriek && current == (byte)'/')
					{
						token = new WhitespaceToken(start, source.Position);
						return true;
					}
					else if (current == (byte)'*')
					{
						if (source.TryPeek(out var next) && next == (byte)'/')
						{
							source.Advance(1);
							token = new WhitespaceToken(start, source.Position);
							return true;
						}
					}

					needsAstriek = true;
				}
			}
			else
			{
				// sigle line, go until line ends

				while (source.TryNext(out var current) && current != (byte)'\n')
				{
					// consume
				}
			}

			token = new WhitespaceToken(start, source.Position);
			return true;
		}
	}
}