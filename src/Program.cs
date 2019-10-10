using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Terumi.Binder;
using Terumi.Lexer;
using Terumi.Targets;
using Terumi.Targets.Python;
using Terumi.Tokens;
using Terumi.Workspace;
using Terumi.Workspace.TypePasser;

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
				KeyValuePair.Create("return", Keyword.Return),
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
			yield return new NumericPattern();
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
			var parser = new Parser.StreamParser();

			var parsedSourceFiles = project.ParseAllSourceFiles(lexer, parser).ToList();

			var binder = new BinderEnvironment(parsedSourceFiles);

			binder.PassOverTypeDeclarations();
			binder.PassOverMembers();

			// now we should be able to infer every type in every code body

#if DEBUG
			var jsonSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(binder.TypeInformation, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings
			{
				ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize,
				PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects
			});
			File.WriteAllText("binder_info.json", jsonSerialized);
#endif
			/*
			var environment = project.ToEnvironment(lexer, parser);

			foreach(var item in environment.OrderByLeastDependencies())
			{
				Console.WriteLine("Would compile item on '" + item.Key.ToString() + "': " + item.Value.Item.ToString());
			}

			var asUnit = new SyntaxTree.CompilerUnit(environment.OrderByLeastDependencies().ToCompilerUnitItems().ToArray());

			var compilationUnit = DefaultBinder.BindToAst(asUnit);

			using var outfs = File.OpenWrite("out.py");
			ILanguageTarget target = new PythonTarget();
			target.Write(outfs, compilationUnit);
			*/
#if FALSE && DEBUG
			var jsonSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(environment, Newtonsoft.Json.Formatting.Indented);
			File.WriteAllText("tokens.json", jsonSerialized);

			jsonSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(compilationUnit, Newtonsoft.Json.Formatting.Indented);
			File.WriteAllText("ast.json", jsonSerialized);
#endif

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

			var tokenizer = new Parser.StreamParser();

			if (!tokenizer.TryParse(tokens, out var compilationUnit))
			{
				Console.WriteLine("Unable to compile");
				return;
			}

			System.IO.File.WriteAllText("token.json", Newtonsoft.Json.JsonConvert.SerializeObject(compilationUnit, Newtonsoft.Json.Formatting.Indented));
		}
	}
}