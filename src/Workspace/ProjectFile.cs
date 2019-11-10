namespace Terumi.Workspace
{
	public class ProjectFile
	{
		public ProjectFile(string path, string source, PackageLevel packageLevel)
		{
			Path = path;
			Source = source;
			PackageLevel = packageLevel;
		}

		public string Source { get; }
		public PackageLevel PackageLevel { get; }
		public string Path { get; }
	}
}