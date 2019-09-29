using System;
using System.IO.Abstractions;

namespace Terumi.Workspace
{
	public class LibraryPuller
	{
		private readonly IFileSystem _fileSystem;
		private readonly string _libraryPath;

		public LibraryPuller(IFileSystem fileSystem, string libraryPath)
		{
			_fileSystem = fileSystem;
			_libraryPath = libraryPath;
		}

		public Project Pull(LibraryReference reference, bool forcePull = false)
		{
			// TODO: replace with a git-based implementation

			if (!Project.TryLoad(_libraryPath, reference.Name, _libraryPath, this, _fileSystem, out var project))
			{
				throw new Exception("Couldn't pull dependency '" + reference.Name + "'.");
			}

			return project;
		}
	}
}