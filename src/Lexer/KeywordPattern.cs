using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Tokenizer;

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
			foreach(var (pattern, keyword) in keywords)
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

		public bool TryParse(ReaderFork source, out Token token)
		{
			Span<byte> buffer = stackalloc byte[_maxSize];

			int i = 0;

			while (source.TryNext(out var value) && i < _maxSize)
			{
				buffer[i++] = value;

				for(var patternIndex = 0; patternIndex < _keywords.Length; patternIndex++)
				{
					Span<byte> pattern = _patterns[patternIndex];
					var keyword = _keywords[patternIndex];

					if (pattern.Length != i)
					{
						continue;
					}

					if (!pattern.SequenceEqual(buffer.Slice(0, i)))
					{
						continue;
					}

					token = new KeywordToken(keyword);
					return true;
				}
			}

			token = default;
			return false;
		}
	}
}
