using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class KeywordPattern : IPattern
	{
		private readonly int _maxSize;
		private readonly byte[][] _patterns;
		private readonly Keyword[] _keywords;

		public KeywordPattern
		(
			KeyValuePair<string, Keyword>[] keywords
		)
		{
			_patterns = new byte[keywords.Length][];
			_keywords = new Keyword[keywords.Length];

			var i = 0;
			foreach (var (pattern, keyword) in keywords)
			{
				var bytes = Encoding.UTF8.GetBytes(pattern);
				_patterns[i] = bytes;
				_keywords[i] = keyword;

				if (bytes.Length > _maxSize)
				{
					_maxSize = bytes.Length;
				}

				i++;
			}
		}

		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			var i = 0;

			for (; i < source.Length; i++)
			{
				var sourceKeyword = source.Slice(0, i);

				for (var patternIndex = 0; patternIndex < _keywords.Length; patternIndex++)
				{
					Span<byte> pattern = _patterns[patternIndex];

					if (pattern.Length != i)
					{
						continue;
					}

					var keyword = _keywords[patternIndex];

					if (!pattern.SequenceEqual(sourceKeyword))
					{
						continue;
					}

					token = new KeywordToken(meta, keyword);
					return i;
				}
			}

			return 0;
		}
	}
}