using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			var fs = new System.IO.Abstractions.FileSystem();

			if (!Project.TryLoad(file, fs, Git.Instance, out var project))
			{
				Console.WriteLine("Couldn't load project.");
				return;
			}

			Console.WriteLine("Loaded project.");

			var lexer = new StreamLexer(GetPatterns());
			var tokenizer = new Tokenizer.Tokenizer();

			var environment = project.ToEnvironment(lexer, tokenizer);

			foreach(var item in environment.OrderByLeastDependencies())
			{
				Console.WriteLine("Would compile item on '" + item.Key.ToString() + "': " + item.Value.Item.ToString());
			}

			return;
		}

		private static void TreeDependencies(Project project, int spacing = 0)
		{
			var prefix = new string(' ', spacing);
			Console.WriteLine($"{prefix}Treeing project '{project.Name}':");

			foreach (var source in project.GetSources())
			{
				Console.WriteLine($"{prefix}Has source: " + source.PackageLevel.Levels[0]);
			}

			foreach (var dependency in project.GetDependencies())
			{
				if (dependency == null)
				{
					Console.WriteLine($"{prefix}Null dependency.");
					continue;
				}

				Console.WriteLine($"{prefix}Treeing dependency '{dependency.Name}'");

				TreeDependencies(dependency, spacing + 1);
			}
		}

		private static void CompileFile(string file)
		{
			using var source = File.OpenRead(file);
			var lexer = new StreamLexer(GetPatterns());
			var tokens = DebugTokenInfo(lexer.ParseTokens(source));

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