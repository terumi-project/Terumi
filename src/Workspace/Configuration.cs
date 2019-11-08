using Nett;

using System;
using System.IO;

namespace Terumi.Workspace
{
	public class Configuration
	{
		public static Configuration Default { get; } = new Configuration
		{
			Libraries = Array.Empty<LibraryReference>()
		};

		public static Configuration ReadFile(string filePath)
		{
			using var stream = File.OpenRead(filePath);
			return Toml.ReadStream<Configuration>(stream);
		}

		[TomlMember(Key = "libs")]
		public LibraryReference[] Libraries { get; set; } = Array.Empty<LibraryReference>();
	}

	public class LibraryReference
	{
		[TomlMember(Key = "projects")]
		public string[] Projects { get; set; }

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