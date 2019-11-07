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
		public static bool TryLoad(string name, IFileSystem fileSystem, out Project project)
		{
			var fullName = fileSystem.Path.GetFullPath(name);

			var basePath = fileSystem.Path.Combine(fullName, "..");
			var libraryPath = fileSystem.Path.Combine(fullName, ".libs");

			return Project.TryLoad(basePath, name, libraryPath, new LibraryPuller(fileSystem, libraryPath), fileSystem, out project);
		}

		public static bool TryLoad(string basePath, string name, string libraryPath, LibraryPuller puller, IFileSystem fileSystem, out Project project)
		{
			var namePath = fileSystem.Path.Combine(basePath, name);

			if (!fileSystem.Directory.Exists(namePath))
			{
				Log.Error($"Path to project '{namePath}' doesn't exist");
				project = default;
				return false;
			}

			var config = Configuration.Default;
			var configName = fileSystem.Path.Combine(basePath, name + ".toml");

			if (fileSystem.File.Exists(configName))
			{
				Log.Debug($"Reading project configuration '{configName}'");
				config = Configuration.ReadFile(configName, fileSystem);
			}

			var anyTerumiFilesInFolder = fileSystem.Directory.GetFiles(namePath)
				.Where(file => file.EndsWith(".trm"));

			if (!anyTerumiFilesInFolder.Any())
			{
				Log.Debug($"Project has no files ('{name}'@'{namePath})");
				project = default;
				return false;
			}

			Log.Debug($"Loaded project '{name}'");
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
			_basePath = _fileSystem.Path.GetFullPath(basePath);
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
				var fullFile = _fileSystem.Path.GetFullPath(file);

				if (!fullFile.StartsWith(_basePath))
				{
					// uhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
					// should never happen :)
					throw new System.Exception("Source file must have a base folder... am i bad programmer?");
				}

				var subPath = fullFile.Substring(_basePath.Length);
				var levelsWithFile = subPath.Split(_fileSystem.Path.DirectorySeparatorChar);
				var levels = levelsWithFile.Take(levelsWithFile.Length - 1).Where(str => !string.IsNullOrWhiteSpace(str)).ToArray();

				var location = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_basePath, file));

				// hope the user disposes it
				var stream = _fileSystem.File.OpenRead(location);
				var sourceFile = new SourceFile(stream, new SyntaxTree.PackageLevel(SyntaxTree.PackageAction.Namespace, levels), location);

				Log.Debug($"Found source file of '{Name}'@'{location}'");
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