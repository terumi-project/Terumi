using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Terumi.Workspace
{
	public class DependencyResolver
	{
		private readonly string _libraryPath;

		public DependencyResolver(string libraryPath)
		{
			_libraryPath = libraryPath;

			if (!Directory.Exists(libraryPath))
			{
				var dirInfo = Directory.CreateDirectory(libraryPath);
				dirInfo.Attributes = FileAttributes.Hidden;
			}
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

		public Project Resolve(LibraryReference reference)
		{
			if (reference.Path != null)
			{
				if (Directory.Exists(reference.Path))
				{
					return Resolve(reference.Path);
				}

				Log.Warn($"Unable to resolve dependency living at '{reference.Path}'. Resorting to using git.");
			}

			// TODO: null check git stuff

			var referenceName = Hash($"{reference.GitUrl}.{reference.Branch}.{reference.CommitId}");
			var libraryPath = Path.Combine(_libraryPath, referenceName);

			if (!Directory.Exists(libraryPath))
			{
				Git.Clone(reference.GitUrl, reference.Branch, reference.CommitId, libraryPath);
			}

			return Resolve(libraryPath);

			static Project Resolve(string projectPath)
			{
				if (!Project.TryLoad(projectPath, out var project))
				{
					throw new DependencyResolveException($"Unable to resolve dependency '{projectPath}'");
				}

				return project;
			}
		}
	}
}