namespace Terumi.Workspace
{
	public class ProjectFile
	{
		public ProjectFile(string source, string[] packageLevel)
		{
			Source = source;
			PackageLevel = packageLevel;
		}

		public string Source { get; }
		public string[] PackageLevel { get; }
	}
}