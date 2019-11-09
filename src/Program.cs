using System.Collections.Generic;
using System.IO;
using System.Linq;

using Terumi.Binder;
using Terumi.Lexer;
using Terumi.Parser;
using Terumi.Targets;
using Terumi.Tokens;
using Terumi.Workspace;

namespace Terumi
{
	internal static class Program
	{
		private static readonly StreamLexer _lexer = new StreamLexer(GetPatterns());
		private static readonly StreamParser _parser = new StreamParser();

		private static IPattern[] GetPatterns()
			=> new IPattern[]
		{
			new CharacterPattern('\n'),
			new WhitespacePattern(),
			new CommentPattern(),

			new KeywordPattern(new KeyValuePair<string, Keyword>[]
			{
				KeyValuePair.Create("contract", Keyword.Contract),
				KeyValuePair.Create("class", Keyword.Class),
				KeyValuePair.Create("readonly", Keyword.Readonly),
				KeyValuePair.Create("namespace", Keyword.Namespace),
				KeyValuePair.Create("using", Keyword.Using),
				KeyValuePair.Create("return", Keyword.Return),
				KeyValuePair.Create("this", Keyword.This),
				KeyValuePair.Create("true", Keyword.True),
				KeyValuePair.Create("false", Keyword.False),
			}),

			new CharacterPattern(';', '@', '=', ',', '.', '(', ')', '[', ']', '{', '}', '+', '-', '/', '*'),

			new IdentifierPattern(IdentifierCase.SnakeCase),
			new IdentifierPattern(IdentifierCase.PascalCase),
			new NumericPattern(),
			new StringPattern(),
		};

		public static bool Compile(string projectName)
		{
			Project project;

			using (var _ = Log.Stage("SETUP", $"Loading project {projectName}"))
			{
				if (!Project.TryLoad(Directory.GetCurrentDirectory(), projectName, out project))
				{
					Log.Error("Unable to load project");
					return false;
				}
			}

			List<ParsedProjectFile> parsedFiles;

			using (var _ = Log.Stage("PARSE", "Parsing project source code"))
			{
				parsedFiles = project.ParseProject(_lexer, _parser)
					.ToList();
			}

			BinderEnvironment binder;

			using (var _ = Log.Stage("BINDING", "Binding parsed source files to in memory representations"))
			{
				binder = new BinderEnvironment(parsedFiles);

				binder.PassOverTypeDeclarations();
				binder.PassOverMembers();
				binder.PassOverMethodBodies();
			}

			using (var _ = Log.Stage("WRITING", "Writing input code to target powershell file."))
			{
				// try/catching to delete files w/ IOException is a good practice
				try { File.Delete("out.ps1"); } catch (IOException __) { }

				// looks ugly but meh
				using var fs = File.OpenWrite("out.ps1");
				using var sw = new StreamWriter(fs);

				var target = new PowershellTarget(binder.TypeInformation);

				foreach (var item in binder.TypeInformation.Binds)
				{
					target.Write(sw, item);
				}

				target.Post(sw);
			}

			return true;
		}

		/// <summary>
		/// Terumi application - WIP
		/// </summary>
		private static void Main(string[] args)
		{
#if DEBUG
			Directory.SetCurrentDirectory("D:\\test");
			Compile("sample_project");
#endif
		}
	}
}