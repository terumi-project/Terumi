using System;
using System.Collections.Generic;
using System.IO;

using Terumi.Lexer;
using Terumi.Tokens;
using Terumi.Workspace;

namespace Terumi
{
	internal class Program
	{
		private static IEnumerable<IPattern> GetPatterns()
		{
			yield return new CharacterPattern('\n');
			yield return new WhitespacePattern();
			yield return new CommentPattern();

			yield return new KeywordPattern(new KeyValuePair<string, Keyword>[]
			{
				KeyValuePair.Create("contract", Keyword.Contract),
				KeyValuePair.Create("class", Keyword.Class),
				KeyValuePair.Create("readonly", Keyword.Readonly),
				KeyValuePair.Create("namespace", Keyword.Namespace),
				KeyValuePair.Create("using", Keyword.Using),
			});

			yield return new CharacterPattern(';');
			yield return new CharacterPattern('@');

			yield return new CharacterPattern('=');
			yield return new CharacterPattern(',');
			yield return new CharacterPattern('.');

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

		private static IEnumerable<Token> DebugTokenInfo(IEnumerable<Token> tokens)
		{
			foreach (var token in tokens)
			{
				Console.WriteLine(token.ToString());
				yield return token;
			}
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
				file = "sample_project";
			}
#endif

			if (!Project.TryLoad(file, new System.IO.Abstractions.FileSystem(), out var project))
			{
				Console.WriteLine("Couldn't load project.");
				return;
			}

			Console.WriteLine("Loaded project.");

			foreach(var lib in project.Configuration.Libraries)
			{
				Console.WriteLine("using lib: " + lib.GitUrl);
				Console.WriteLine(lib.CommitId);
			}

			foreach(var sourceFile in project.GetSources())
			{

			}

			return;
		}

		private static void CompileFile(string file)
		{
			using var source = File.OpenRead(file);
			var lexer = new StreamLexer(source, GetPatterns());
			var tokens = DebugTokenInfo(lexer.ParseTokens());

			var tokenizer = new Tokenizer.Tokenizer();

			if (!tokenizer.TryParse(tokens, out var compilationUnit))
			{
				Console.WriteLine("Unable to compile");
				return;
			}

			System.IO.File.WriteAllText("token.json", Newtonsoft.Json.JsonConvert.SerializeObject(compilationUnit, Newtonsoft.Json.Formatting.Indented));
		}
	}
}