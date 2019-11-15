using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Terumi.Binder;
using Terumi.Lexer;
using Terumi.Parser;
using Terumi.Targets;
using Terumi.Tokens;
using Terumi.VarCode;
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
				KeyValuePair.Create("if", Keyword.If),
				KeyValuePair.Create("this", Keyword.This),
				KeyValuePair.Create("true", Keyword.True),
				KeyValuePair.Create("using", Keyword.Using),
				KeyValuePair.Create("false", Keyword.False),
				KeyValuePair.Create("class", Keyword.Class),
				KeyValuePair.Create("return", Keyword.Return),
				KeyValuePair.Create("contract", Keyword.Contract),
				KeyValuePair.Create("readonly", Keyword.Readonly),
				KeyValuePair.Create("namespace", Keyword.Namespace),
			}),

			new CharacterPattern(';', '@', '=', ',', '.', '(', ')', '[', ']', '{', '}', '+', '-', '/', '*'),

			new IdentifierPattern(IdentifierCase.SnakeCase),
			new IdentifierPattern(IdentifierCase.PascalCase),
			new NumericPattern(),
			new StringPattern(),
		};

		public static bool Compile(string projectName, ICompilerMethods setupTarget)
		{
			var resolver = new DependencyResolver(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), projectName, ".libs")));

			Log.Stage("SETUP", $"Loading project {projectName}");
			if (!Project.TryLoad(Directory.GetCurrentDirectory(), projectName, out var project))
			{
				Log.Error("Unable to load project");
				return false;
			}

			Log.Stage("PARSE", "Parsing project source code");
			var parsedFiles = project.ParseProject(_lexer, _parser, resolver).ToList();

			Log.Stage("BINDING", "Binding parsed source files to in memory representations");

			var binder = new BinderEnvironment(setupTarget, parsedFiles);
			binder.PassOverTypeDeclarations();
			binder.PassOverMethodBodies();

			Log.Stage("OPTIMIZATION", "Optimizing TypeInformation");

			var translator = new VarCodeTranslator();
			translator.Visit(binder.TypeInformation.Binds);
			var translation = translator.GetTranslation();

			File.WriteAllText("t.txt", Newtonsoft.Json.JsonConvert.SerializeObject(translation, Newtonsoft.Json.Formatting.Indented));

			// var optimizer = new VarCode.Optimizer.Alpha.VarCodeOptimizer(translation);
			// optimizer.Optimize();

			File.WriteAllText("o.txt", Newtonsoft.Json.JsonConvert.SerializeObject(translation, Newtonsoft.Json.Formatting.Indented));

			// Optimizer.Optimize(binder.TypeInformation);

			Log.Stage("WRITING", "Writing input code to target powershell file.");

			// try/catching to delete files w/ IOException is a good practice
			try { File.Delete("out.ps1"); } catch (IOException __) { }

			// looks ugly but meh
			using var fs = File.OpenWrite("out.ps1");
			using var sw = new StreamWriter(fs);

			// tabs <3
			using var indentedWriter = new IndentedTextWriter(sw, "\t");

			var target = setupTarget.MakeTarget(binder.TypeInformation); // new PowershellTarget(binder.TypeInformation);

			foreach (var item in binder.TypeInformation.Binds)
			{
				target.Write(indentedWriter, item);
			}

			target.Post(indentedWriter);

			Log.StageEnd();
			return true;
		}

		/// <summary>
		/// Terumi application - WIP
		/// </summary>
		private static void Main(string[] args)
		{
#if DEBUG
			Directory.SetCurrentDirectory("D:\\test");
			Compile("sample_project", new PowershellMethods());
#endif
		}
	}
}