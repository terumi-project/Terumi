namespace Terumi.Workspace
{
	public class ProjectFile
	{
		public ProjectFile(string path, string source, string[] packageLevel)
		{
			Path = path;
			Source = source;
			PackageLevel = packageLevel;
		}

		public string Source { get; }
		public string[] PackageLevel { get; }
		public string Path { get; }
	}
}