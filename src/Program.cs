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
using Terumi.VarCode.Optimizer.Alpha;
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

		private static IOptimization[] GetOptimizations()
			=> new IOptimization[]
			{
				new RemoveAllUnreferencedMethodsOptimization(),
				new MethodInliningOptimization(),
				new VariableInliningOptimization(),
				new BodyFoldingOptimization(),
				new RemoveAllUnreferencedVariablesOptimization(),
			};

		public static bool Compile(string projectName, ICompilerTarget target)
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

			var binder = new BinderEnvironment(target, parsedFiles);
			binder.PassOverTypeDeclarations();
			binder.PassOverMethodBodies();

			Log.Stage("OPTIMIZATION", "Optimizing TypeInformation");

			var translator = new VarCodeTranslator(binder.TypeInformation.Main);
			translator.Visit(binder.TypeInformation.Binds);
			var store = translator.Store;

			var optimizer = new VarCodeOptimizer(store, GetOptimizations());
			optimizer.Optimize();

			// now we need to convert the store to an omega store
			var omegaStore = VarCode.Optimizer.Omega.VarCodeTranslator.Translate(store);
			// TODO: omega store optimizations

			// Optimizer.Optimize(binder.TypeInformation);

			Log.Stage("WRITING", "Writing input code to target powershell file.");

			// try/catching to delete files w/ IOException is a good practice
			try { File.Delete("out.ps1"); } catch (IOException __) { }

			// looks ugly but meh
			using var fs = File.OpenWrite("out.ps1");
			using var sw = new StreamWriter(fs);

			// tabs <3
			using var indentedWriter = new IndentedTextWriter(sw, "\t");
			target.Write(indentedWriter, omegaStore);

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
			Compile("sample_project", new PowershellTarget());
#endif
		}
	}
}