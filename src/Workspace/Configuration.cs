using Nett;

using System;
using System.IO.Abstractions;

namespace Terumi.Workspace
{
	public class Configuration
	{
		public static Configuration Default { get; } = new Configuration
		{
			Libraries = Array.Empty<LibraryReference>()
		};

		public static Configuration ReadFile(string filePath, IFileSystem fileSystem)
		{
			using var stream = fileSystem.File.OpenRead(filePath);
			return Toml.ReadStream<Configuration>(stream);
		}

		[TomlMember(Key = "libs")]
		public LibraryReference[] Libraries { get; set; }
	}

	public class LibraryReference
	{
		// TODO: NOT PERMANENT
		// shouldn't need this in the final form
		[TomlMember(Key = "name")]
		public string Name { get; set; }

		[TomlMember(Key = "git_url")]
		public string GitUrl { get; set; }

		[TomlMember(Key = "commit")]
		public string CommitId { get; set; }
	}
}