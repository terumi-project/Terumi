namespace Terumi.Workspace
{
	public class ProjectFile
	{
		public ProjectFile(Project project, string path, string source, PackageLevel packageLevel)
		{
			Project = project;
			Path = path;
			Source = source;
			PackageLevel = packageLevel;
		}

		public string Source { get; }
		public PackageLevel PackageLevel { get; }
		public Project Project { get; }
		public string Path { get; }
	}
}