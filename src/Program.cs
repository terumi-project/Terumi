using System;
using System.Collections.Generic;
using System.IO;

using Terumi.Lexer;
using Terumi.Tokens;

namespace Terumi
{
	internal class Program
	{
		private static IEnumerable<IPattern> GetPatterns()
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

			yield return new IdentifierPattern(IdentifierCase.SnakeCase);
			yield return new IdentifierPattern(IdentifierCase.PascalCase);
		}

		/// <summary>
		/// Terumi application - WIP
		/// </summary>
		private static void Main(string[] args)
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
			var tokens = lexer.ParseTokens();

			var tokenizer = new Tokenizer.Tokenizer();

			if (!tokenizer.TryParse(tokens, out var compilationUnit))
			{
				Console.WriteLine("Unable to compile");
				return;
			}

			return;
		}
	}
}