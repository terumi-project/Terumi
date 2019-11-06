using System;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Terumi.Workspace
{
	public class LibraryPuller
	{
		private readonly IFileSystem _fileSystem;
		private readonly string _libraryPath;
		private readonly IGit _git;

		public LibraryPuller(IFileSystem fileSystem, string libraryPath, IGit git)
		{
			_fileSystem = fileSystem;
			_libraryPath = libraryPath;
			_git = git;
		}

		private static string Hash(string input)
		{
			using (var managed = new SHA256Managed())
			{
				var computedBytes = managed.ComputeHash(Encoding.UTF8.GetBytes(input));

				var strb = new StringBuilder(computedBytes.Length * 2);

				foreach (var computedByte in computedBytes)
				{
					strb.AppendFormat("{0:x2}", computedByte);
				}

				return strb.ToString();
			}
		}

		public Project[] Pull(LibraryReference reference, bool forcePull = false)
		{
			var name = Hash(reference.GitUrl + "." + reference.Branch + "." + reference.CommitId);
			var libHome = _fileSystem.Path.Combine(_libraryPath, name);
			var libExists = _fileSystem.Directory.Exists(libHome);

			// TODO: decide the best way to pull crud, w/ forcePull
			if (reference.Path != null && _fileSystem.Directory.Exists(reference.Path))
			{
				libHome = _fileSystem.Path.GetFullPath(reference.Path);
			}
			else if (forcePull)
			{
				Repull();
			}
			else if (!libExists)
			{
				Repull();
			}

			var projects = new Project[reference.Projects.Length];

			for (var i = 0; i < projects.Length; i++)
			{
				var projectName = reference.Projects[i];

				if (!Project.TryLoad(libHome, projectName, _libraryPath, this, _fileSystem, out var project))
				{
					// TODO: exception on loading project
					Console.WriteLine("Couldn't load " + projectName);
				}

				projects[i] = project;
			}

			return projects;

			void Repull()
			{
				if (_fileSystem.Directory.Exists(libHome))
				{
					_fileSystem.Directory.Delete(libHome, true);
				}

				_git.Clone(reference.GitUrl, reference.Branch, reference.CommitId, libHome);
			}
		}
	}
}