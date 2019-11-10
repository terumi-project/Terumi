﻿using System;
using System.Text;

using Terumi.Tokens;

namespace Terumi.Lexer
{
	public class StringPattern : IPattern
	{
		public int TryParse(Span<byte> source, LexerMetadata meta, ref IToken token)
		{
			if (source[0] != '"')
			{
				return 0;
			}

			var strb = new StringBuilder();

			var initialValue = 1;
			var i = initialValue;

			for (; i < source.Length; i++)
			{
				var current = source[i];

				// ignore all '\r's
				if (current == '\r')
				{
					// increment initialValue so that we can still skip the first \n
					initialValue++;
					continue;
				}

				// if the string starts with a '\n', we want to ignore that
				/*
				string so = "
				that multiline strings ignore the first newline"
				*/
				if (i == initialValue && current == '\n')
				{
					continue;
				}

				if (current == '\\')
				{
					if (i + 1 < source.Length)
					{
						throw new Exception($"File ended on a backslash in the middle of a string. String began {meta} String ended {meta.FromConsumed(source.Slice(0, i))}");
					}

					var next = source[++i];

					switch (next)
					{
						case (byte)'n': strb.Append('\n'); continue;
						case (byte)'t': strb.Append('\t'); continue;
						case (byte)'\\': strb.Append('\\'); continue;
						default: throw new LexingException($"Unexpected escape sequence in string '\\{next}' {meta.FromConsumed(source.Slice(0, i))}.");
					}
				}

				if (current == '"')
				{
					token = new StringToken(meta, strb.ToString());
					return i + 1;
				}

				// TODO: could optimize this if we needed to, but EH
				strb.Append((char)current);
			}

			throw new Exception($"String didn't end. String began {meta} String ended {meta.FromConsumed(source.Slice(0, i))}");
		}
	}
}