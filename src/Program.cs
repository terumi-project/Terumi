using System;
using System.Collections.Generic;
using System.IO;
using Terumi.Lexer;
using Terumi.Tokenizer;

namespace Terumi
{
	internal class Program
	{
		static IEnumerable<IPattern> GetPatterns()
		{
			yield return new CharacterPattern('\n');
			yield return new WhitespacePattern();

			yield return new KeywordPattern(new KeyValuePair<string, Keyword>[]
			{
				KeyValuePair.Create("contract", Keyword.Contract),
				KeyValuePair.Create("class", Keyword.Class),
			});

			yield return new CharacterPattern(';');
			yield return new CharacterPattern('@');

			yield return new CharacterPattern('=');

			yield return new CharacterPattern('(');
			yield return new CharacterPattern(')');

			yield return new CharacterPattern('[');
			yield return new CharacterPattern(']');

			yield return new CharacterPattern('{');
			yield return new CharacterPattern('}');

			yield return new CharacterPattern('+');
			yield return new CharacterPattern('-');
			yield return new CharacterPattern('/');
			yield return new CharacterPattern('*');
		}

		/// <summary>
		/// Terumi application - WIP
		/// </summary>
		static void Main(string[] args)
		{
			string file = default;
#if DEBUG
			if (file == default)
			{
				file = "test.txt";
			}
#endif

			using var source = File.OpenRead(file);
			var lexer = new StreamLexer(source, GetPatterns());

			foreach (var token in lexer.ParseTokens())
			{
				Console.WriteLine("Token: " + token.GetType().FullName);
			}
		}
	}
}