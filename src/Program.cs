using Nett;
using System;
using System.CodeDom.Compiler;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terumi.Binder;
using Terumi.CodeSources;
using Terumi.Parser;
using Terumi.Targets;
using Terumi.VarCode;
using Terumi.Workspace;

namespace Terumi
{
	public enum Target
	{
		Powershell,
		Bash,
		C,
	}

	internal static class Program
	{
		public static async Task<bool> Compile(ICompilerTarget target)
		{
			Log.Stage("SETUP", $"Loading project @'{Directory.GetCurrentDirectory()}'");
			if (!Project.TryLoad(Directory.GetCurrentDirectory(), out var project))
			{
				Log.Error("Unable to load project");
				return false;
			}

			var resolver = project.CreateResolver();

			// we use 'interpret' to refer to lexing, parsing, and binding
			Log.Stage("INTERPRET", "Reading and interpreting project");

			var workspaceParser = new WorkspaceParser(resolver, target);

			if (!workspaceParser.TryParse(project, out var bindings))
			{
				Log.Error("Unable to interpret workspace");
				return false;
			}

			Log.StageEnd();

			var flat = new Flattening.Flattener(bindings);
			var flattened = flat.Flatten();

			var deobj = new Deobjectification.Deobjectifier(bindings, flattened, target);
			var translated = deobj.Translate(out var objectFields);

			var optimized = await VarCode.Optimization.PruneMethods.UsedMethods(translated);

			Log.Stage("WRITING", "Writing code to output.");

			var bin = $"{Directory.GetCurrentDirectory()}/bin";
			var outFile = $"{bin}/{target.ShellFileName}";

			if (!Directory.Exists(bin)) Directory.CreateDirectory(bin);

			// try/catching to delete files w/ IOException is a good practice
			try { File.Delete(outFile); } catch (IOException) { }

			// looks ugly but meh
			using var fs = File.OpenWrite(outFile);
			using var sw = new StreamWriter(fs);

			// tabs <3
			using var indentedWriter = new IndentedTextWriter(sw, "\t");
			target.Write(indentedWriter, optimized, objectFields);

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
				new Option(new string[] { "--target", "-t" }, "Target language to compile into")
				{
					Required = true,
					Argument = new Argument<Target>()
				}
			};

			var installCommand = new Command("install", "Installs a terumi package from a package source")
			{
				new Option(new string[] { "--package-name", "-p" }, "Name of the package")
				{
					Required = true,
					Argument = new Argument<string>()
				},

				new Option(new string[] { "--version", "-v" }, "Version of the project")
				{
					Required = false,
					Argument = new Argument<string>()
				}

				// TODO: configurable package source
			};

			var rootCommand = new RootCommand("Terumi Compiler")
			{
				newCommand,
				compileCommand,
				installCommand
			};

			newCommand.Handler = CommandHandler.Create<string>(NewProject);
			compileCommand.Handler = CommandHandler.Create<Target>(CompileProject);
			installCommand.Handler = CommandHandler.Create<string, string>(InstallProject);

#if false && DEBUG
			Directory.SetCurrentDirectory("test");
			return Compile(new CTarget()).ContinueWith(t => Task<int>.FromResult(0)).GetAwaiter().GetResult();
			return Task<int>.FromResult(0);
			// return rootCommand.InvokeAsync(new string[] { "compile", "-t", "c" });
#else
			return rootCommand.InvokeAsync(args);
#endif
		}

		private static void NewProject(string name)
		{
			Log.Stage("GIT", "Initializing git repository");
			var targetDirectory = Path.GetFullPath(name);

			Git.Init(targetDirectory);
			Log.StageEnd();

			Log.Stage("NEW", $"Creating project {name}");

			/*
			 * Project Hierarchy:
			 * 
			 * The target project folder will be a git repo
			 * /{name}/.git/...
			 * 
			 * The bin folder is for build artifacts
			 * Debug mode artifacts
			 * /{name}/bin/debug/out.ps1
			 * /{name}/bin/debug/out.sh
			 * 
			 * Release mode artifacts
			 * /{name}/bin/release/out.ps1
			 * /{name}/bin/release/out.sh
			 * 
			 * The .libs folder is for dependency pulling
			 * /{name}/.libs/...
			 * 
			 * The src folder is for source code
			 * The project name is there for inferred namespaces,
			 * so main.trm has the namespace `name` by default.
			 * /{name}/src/{name}/main.trm
			 * 
			 * Tests, if there should be any
			 * /{name}/tests/main.trm
			 * 
			 * Configuration file
			 * /{name}/config.toml
			 * 
			 * Gitignore
			 * /{name}/.gitignore
			 */

			var configurationFile = Path.Combine(targetDirectory, "config.toml");
			var gitignoreFile = Path.Combine(targetDirectory, ".gitignore");

			var srcDirectory = Path.Combine(targetDirectory, "src");
			var inferredNamespaces = name.Split('.').Prepend(srcDirectory).Aggregate(Path.Combine);
			var mainFile = Path.Combine(inferredNamespaces, "main.trm");

			var testsDirectory = Path.Combine(targetDirectory, "tests");
			var testsMainFile = Path.Combine(testsDirectory, "main.trm");

			Directory.CreateDirectory(targetDirectory);
			Directory.CreateDirectory(srcDirectory);
			Directory.CreateDirectory(inferredNamespaces);
			Directory.CreateDirectory(testsDirectory);

			File.WriteAllText(gitignoreFile, @"# ignore build artifacts
bin

# ignore cached pulled dependencies
.libs
");

			File.WriteAllText(mainFile, @"main()
{
	@println(""Hello, World!"")
}
");

			File.WriteAllText(testsMainFile, @"// TODO: get tests working
");

			var load = Project.TryLoad(targetDirectory, out var project);
			Debug.Assert(load);

			Configuration.Save(project.Configuration, configurationFile);

			Log.StageEnd();
		}

		private static void CompileProject(Target target)
		{
			ICompilerTarget compilerTarget;

			switch (target)
			{
				case Target.Bash: compilerTarget = new BashTarget(); break;
				case Target.Powershell: compilerTarget = new PowershellTarget(); break;
				case Target.C: compilerTarget = new CTarget(); break;
				default: throw new InvalidOperationException();
			}

			Log.Info($"Could compile project: {Compile(compilerTarget)}");
		}

		private static async Task InstallProject(string packageName, string? version)
		{
			Log.Stage("LOAD", "Loading project");

			if (!Project.TryLoad(Directory.GetCurrentDirectory(), out var project))
			{
				Log.Error($"Unable to load project @'{Directory.GetCurrentDirectory()}'");
				return;
			}

			Log.StageEnd();

			Log.Stage("PULL", "Downloading package from repository");
			var package = await Source.Instance.Fetch(packageName);

			if (package == null)
			{
				Log.Error($"Unable to install package {packageName}");
				return;
			}

			Log.Info($"Found '{package.Name}', by {package.Author}: {package.Description}");
			Log.Info($"{package.Snapshots.Length} versions available - most recent: v{package.Snapshots[0].Version}");

			ToolSnapshot snapshot;

			if (string.IsNullOrWhiteSpace(version))
			{
				// the first snapshot should be the most up to date
				snapshot = package.Snapshots.First();

				Log.Info($"No version specified - using v{snapshot.Version}");
			}
			else
			{
				snapshot = package.Snapshots.FirstOrDefault(snapshot => snapshot.Version == version);

				if (snapshot == null)
				{
					Log.Error($"Unable to find v{version}");
					return;
				}

				Log.Info($"Using version v{snapshot.Version}");
			}

			Log.StageEnd();

			Log.Stage("ADD", "Adding package to project");
			Log.Info("Adding project to configuration file");
			var newConfig = PackageRewriter.Add(project.Configuration, snapshot);
			Toml.WriteFile(newConfig, project.ConfigurationPath);
			Log.StageEnd();
		}
	}
}