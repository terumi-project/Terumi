using System.Collections.Generic;
using System.IO;
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
		public static IEnumerable<Project> LoadAll(IFileSystem fileSystem)
		{
			foreach (var directory in fileSystem.Directory.GetDirectories(fileSystem.Directory.GetCurrentDirectory()))
			{
				if (TryLoad(directory, fileSystem, out var project))
				{
					yield return project;
				}
			}
		}

		public static bool TryLoad(string name, IFileSystem fileSystem, out Project project)
		{
			if (!fileSystem.Directory.Exists(name))
			{
				project = default;
				return false;
			}

			var config = Configuration.Default;
			var configName = name + ".toml";

			if (fileSystem.File.Exists(configName))
			{
				config = Configuration.ReadFile(configName, fileSystem);
			}

			var anyTerumiFilesInFolder = fileSystem.Directory.GetFiles(name)
				.Where(file => file.EndsWith(".trm"));

			if (!anyTerumiFilesInFolder.Any())
			{
				project = default;
				return false;
			}

			project = new Project(config, fileSystem, anyTerumiFilesInFolder.ToArray());
			return true;
		}

		private readonly IFileSystem _fileSystem;
		private readonly string[] _files;

		public Project(Configuration configuration, IFileSystem fileSystem, string[] files)
		{
			Configuration = configuration;
			_fileSystem = fileSystem;
			_files = files;
		}

		public Configuration Configuration { get; }

		public IEnumerable<SourceFile> GetSources()
		{
			foreach(var file in _files)
			{
				using var stream = _fileSystem.File.OpenRead(file);
				using var sourceFile = new SourceFile(stream, default);

				System.Console.WriteLine("todo: conv to packagelevel " + file);

				yield return sourceFile;
			}
		}
	}
}