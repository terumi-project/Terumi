using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Terumi.Workspace
{
	// folder /
	// folder / project_name / <source files.trm>
	// folder / project_name.toml
	// folder / project_name / .libs / <library source codes and toml files>

	public class Project
	{
		public static bool TryLoad(string name, IFileSystem fileSystem, IGit git, out Project project)
		{
			var fullName = fileSystem.Path.GetFullPath(name);

			var basePath = fileSystem.Path.Combine(fullName, "..");
			var libraryPath = fileSystem.Path.Combine(fullName, ".libs");

			return Project.TryLoad(basePath, name, libraryPath, new LibraryPuller(fileSystem, libraryPath, git), fileSystem, out project);
		}

		public static bool TryLoad(string name, LibraryPuller puller, IFileSystem fileSystem, out Project project)
		{
			var fullName = fileSystem.Path.GetFullPath(name);

			var basePath = fileSystem.Path.Combine(fullName, "..");
			var libraryPath = fileSystem.Path.Combine(fullName, ".libs");

			return Project.TryLoad(basePath, name, libraryPath, puller, fileSystem, out project);
		}

		public static bool TryLoad(string basePath, string name, string libraryPath, LibraryPuller puller, IFileSystem fileSystem, out Project project)
		{
			var namePath = fileSystem.Path.Combine(basePath, name);

			if (!fileSystem.Directory.Exists(namePath))
			{
				project = default;
				return false;
			}

			var config = Configuration.Default;
			var configName = fileSystem.Path.Combine(basePath, name + ".toml");

			if (fileSystem.File.Exists(configName))
			{
				config = Configuration.ReadFile(configName, fileSystem);
			}

			var anyTerumiFilesInFolder = fileSystem.Directory.GetFiles(namePath)
				.Where(file => file.EndsWith(".trm"));

			if (!anyTerumiFilesInFolder.Any())
			{
				project = default;
				return false;
			}

			project = new Project(puller, config, fileSystem, name, basePath, anyTerumiFilesInFolder.ToArray(), libraryPath);
			return true;
		}

		private readonly IFileSystem _fileSystem;
		private readonly string[] _files;
		private readonly string _basePath;
		private readonly LibraryPuller _puller;

		public Project
		(
			LibraryPuller puller,
			Configuration configuration,
			IFileSystem fileSystem,
			string name,
			string basePath,
			string[] files,
			string libraryPath
		)
		{
			Configuration = configuration;
			_fileSystem = fileSystem;
			_files = files;
			LibraryPath = libraryPath;
			_basePath = basePath;
			Name = name;
			_puller = puller;
		}

		public string Name { get; }
		public Configuration Configuration { get; }
		public string LibraryPath { get; }

		public IEnumerable<SourceFile> GetSources()
		{
			foreach (var file in _files)
			{
				using var stream = _fileSystem.File.OpenRead(_fileSystem.Path.Combine(_basePath, file));
				using var sourceFile = new SourceFile(stream, new Ast.PackageLevel(Ast.PackageAction.Namespace, new string[1] { file }));

				yield return sourceFile;
			}
		}

		public IEnumerable<Project> GetDependencies()
		{
			foreach (var library in Configuration.Libraries)
			{
				foreach (var dependency in _puller.Pull(library))
				{
					yield return dependency;
				}
			}
		}
	}
}