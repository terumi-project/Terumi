using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Terumi.Workspace
{
	public class Project
	{
		public static bool TryLoad(string path, out Project project)
		{
			project = default;
			var projectPath = Path.GetFullPath(path);

			Log.Info($"Loading project @'{projectPath}");

			if (!Directory.Exists(projectPath))
			{
				Log.Error($"Path doesn't exist: '{projectPath}'");
				return false;
			}

			var projectName = Path.GetFileName(projectPath);

			var configurationPath = Path.Combine(projectPath, "config.toml");
			var gitignorePath = Path.Combine(projectPath, ".gitignore");
			var srcPath = Path.Combine(projectPath, "src");
			var testsPath = Path.Combine(projectPath, "tests");
			var libsPath = Path.Combine(projectPath, ".libs");
			var binPath = Path.Combine(projectPath, "bin");

			if (!Directory.Exists(srcPath))
			{
				Log.Error($"Source path doesn't exist: '{srcPath}'");
				return false;
			}

			var config = Configuration.Default;

			if (File.Exists(configurationPath))
			{
				Log.Info($"Reading config for '{projectName}'@'{configurationPath}'");
				config = Configuration.ReadFile(configurationPath);
				Log.Info($"Read config");
			}

			project = new Project
			(
				projectPath: projectPath,
				projectName: projectName,
				srcPath: srcPath,

				binPath: binPath,
				libsPath: libsPath,

				configurationPath: configurationPath,
				configuration: config
			);

			return true;
		}

		public Project
		(
			string projectPath,
			string projectName,
			string srcPath,

			string binPath,
			string libsPath,

			string configurationPath,
			Configuration configuration
		)
		{
			ProjectPath = projectPath;
			ProjectName = projectName;
			SrcPath = srcPath;

			BinPath = binPath;
			LibsPath = libsPath;

			ConfigurationPath = configurationPath;
			Configuration = configuration;
		}

		public string ProjectPath { get; }
		public string ProjectName { get; }
		public string SrcPath { get; }

		public string BinPath { get; }
		public string LibsPath { get; }

		public string ConfigurationPath { get; }
		public Configuration Configuration { get; }

		public DependencyResolver CreateResolver() => new DependencyResolver(LibsPath);

		public IEnumerable<Project> ResolveDependencies(DependencyResolver resolver)
		{
			foreach (var dependency in Configuration.Libraries)
			{
				yield return resolver.Resolve(dependency);
			}
		}

		public IEnumerable<ProjectFile> GetSources()
		{
			foreach (var file in GetSourceFiles())
			{
				var source = File.ReadAllText(file);

				// extract out the base path from the file path
				var packageLevel =

					// take out the base path to the project
					file.Substring(ProjectPath.Length)

					// now we should have something like 'terumi/json/reader.trm'
					.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)

					// ensure no empty ones
					// e.g. if we substring /a/b/c/proj to /c/proj we will end up having "" "c" "proj"
					.Where(x => !string.IsNullOrEmpty(x))
					.ToArray()

					// get rid of 'reader.trm'
					.ExcludeLast();

				yield return new ProjectFile(file, source, packageLevel);
			}
		}

		private IEnumerable<string> GetSourceFiles()
		{
			foreach (var file in RecursivelySearch(SrcPath))
			{
				if (Path.GetExtension(file) == $".trm")
				{
					yield return file;
				}
			}
		}

		private static IEnumerable<string> RecursivelySearch(string folder)
		{
			foreach (var file in Directory.GetFiles(folder))
			{
				yield return file;
			}

			foreach (var directory in Directory.GetDirectories(folder))
			{
				foreach (var file in RecursivelySearch(directory))
				{
					yield return file;
				}
			}
		}
	}
}