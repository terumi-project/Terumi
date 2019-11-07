using LibGit2Sharp;

using System.Linq;

namespace Terumi
{
	public static class Git
	{
		public static string Clone(string gitUrl, string branch, string commitHash, string path)
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