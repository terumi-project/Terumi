using LibGit2Sharp;

using System.Linq;

namespace Terumi
{
	public interface IGit
	{
		/// <returns>The folder name pointing to the .git folder location</returns>
		string Clone(string gitUrl, string branch, string commitHash, string destinationPath);
	}

	public class Git : IGit
	{
		public static IGit Instance { get; } = new Git();

		public string Clone(string gitUrl, string branch, string commitHash, string path)
		{
			var cloned = Repository.Clone(gitUrl, path, new CloneOptions
			{
				BranchName = string.IsNullOrWhiteSpace(branch) ? "master" : branch,
			});

			var repo = new Repository(cloned);

			if (!string.IsNullOrWhiteSpace(commitHash))
			{
				repo.Reset(ResetMode.Hard, repo.Commits.First(commit => commit.Sha == commitHash));
				// TODO: print ERROR saying they should specify a commit
			}

			return cloned;
		}
	}
}