using LibGit2Sharp;

using System.Linq;

namespace Terumi
{
	public static class Git
	{
		public static string Clone(string gitUrl, string? branch, string? commitHash, string path)
		{
			var branchName = string.IsNullOrWhiteSpace(branch) ? "master" : branch;

			Log.Debug($"Cloning git repository '{gitUrl}' on branch '{branchName}' on commit '{commitHash ?? "N/A"}' at '{path}'");

			var cloned = Repository.Clone(gitUrl, path, new CloneOptions
			{
				BranchName = branchName,
			});

			using var repo = new Repository(cloned);

			if (!string.IsNullOrWhiteSpace(commitHash))
			{
				repo.Reset(ResetMode.Hard, repo.Commits.First(commit => commit.Sha == commitHash));
				// TODO: print ERROR saying they should specify a commit
				Log.Warn($"Please specify a commit to pull the repository '{gitUrl}` from!");
			}

			return cloned;
		}
	}
}