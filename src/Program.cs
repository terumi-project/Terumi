using System;
using System.CodeDom.Compiler;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terumi.Binder;
using Terumi.Parser;
using Terumi.Targets;
using Terumi.VarCode;
using Terumi.Workspace;

namespace Terumi
{
	public enum Target
	{
		Powershell,
		Bash
	}

	internal static class Program
	{
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

			var terumiProject = project.ParseProject(resolver, target);
			Console.WriteLine(terumiProject);

			var translator = new Translator(target);

			foreach (var item in terumiProject.IndirectDependencies
				.Concat(terumiProject.DirectDependencies)
				.Concat(terumiProject.BoundProjectFiles))
			{
				translator.TranslateLight(item);
			}

			translator.TranslateHard();

			Log.Stage("WRITING", "Writing code to output.");

			var bin = $"{projectName}/bin";
			var outFile = $"{bin}/{target.ShellFileName}";

			if (!Directory.Exists(bin)) Directory.CreateDirectory(bin);

			// try/catching to delete files w/ IOException is a good practice
			try { File.Delete(outFile); } catch (IOException __) { }

			// looks ugly but meh
			using var fs = File.OpenWrite(outFile);
			using var sw = new StreamWriter(fs);

			// tabs <3
			using var indentedWriter = new IndentedTextWriter(sw, "\t");
			target.Write(indentedWriter, translator._diary.Methods);

			Log.StageEnd();
			return true;
		}

		private static Task<int> Main(string[] args)
		{
			var newCommand = new Command("new", "Creates a new, blank terumi project")
			{
				new Option(new string[] { "--name", "-n" }, "Name of the project")
				{
					Required = true,
					Argument = new Argument<string>()
				}
			};

			var compileCommand = new Command("compile", "Compiles a terumi project")
			{
				new Option(new string[] { "--name", "-n" }, "Name of the project")
				{
					Required = true,
					Argument = new Argument<string>()
				},

				new Option(new string[] { "--target", "-t" }, "Target language to compile into")
				{
					Required = true,
					Argument = new Argument<Target>()
				}
			};

			var rootCommand = new RootCommand("Terumi Compiler")
			{
				newCommand,
				compileCommand
			};

			newCommand.Handler = CommandHandler.Create<string>(NewProject);
			compileCommand.Handler = CommandHandler.Create<string, Target>(CompileProject);

#if false && DEBUG
			return rootCommand.InvokeAsync(new string[] { "compile", "-n", "radical_thing", "-t", "bash" });
#else
			return rootCommand.InvokeAsync(args);
#endif
		}

		private static void NewProject(string name)
		{
			Log.Stage("Creating project", name);

			File.WriteAllText($"{name}.toml", @"# Include dependencies here!");
			Directory.CreateDirectory(name);

			File.WriteAllText($"{name}/main.trm", @"main()
{
	@println(""Hello, World!"")
}");

			Log.StageEnd();
		}

		private static void CompileProject(string name, Target target)
		{
			ICompilerTarget compilerTarget;

			switch (target)
			{
				case Target.Bash: compilerTarget = new BashTarget(); break;
				case Target.Powershell: compilerTarget = new PowershellTarget(); break;
				default: throw new InvalidOperationException();
			}

			Log.Info($"Could compile project: {Compile(name, compilerTarget)}");
		}
	}
}