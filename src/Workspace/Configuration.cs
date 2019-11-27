using Nett;

using System;
using System.IO;

namespace Terumi.Workspace
{
	public class Configuration
	{
		public static Configuration Default(string name)
			=> new Configuration
		{
			Name = name,
			Libraries = Array.Empty<LibraryReference>()
		};

		public static Configuration ReadFile(string filePath)
		{
			using var stream = File.OpenRead(filePath);
			var config = Toml.ReadStream<Configuration>(stream);

			if (config.Name == null)
			{
				// will turn `/a/b/config.toml`
				// into `/a/b`
				// then into `b`
				config.Name = Path.GetFileName(Path.GetDirectoryName(filePath));
			}

			return config;
		}

		public static void Save(Configuration config, string filePath)
		{
			using var stream = File.OpenWrite(filePath);
			Toml.WriteStream(config, stream);
		}

		[TomlMember(Key = "name")]
		public string Name { get; set; }

		[TomlMember(Key = "libs")]
		public LibraryReference[] Libraries { get; set; } = Array.Empty<LibraryReference>();
	}

	public class LibraryReference
	{
		[TomlMember(Key = "project_path")]
		public string ProjectPath { get; set; }

		[TomlMember(Key = "git_url")]
		public string GitUrl { get; set; }

		[TomlMember(Key = "branch")]
		public string Branch { get; set; }

		[TomlMember(Key = "commit")]
		public string CommitId { get; set; }

		[TomlMember(Key = "path")]
		public string Path { get; set; }
	}
}