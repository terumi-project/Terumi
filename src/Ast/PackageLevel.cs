namespace Terumi.Ast
{
	public enum PackageAction
	{
		Namespace,
		Using
	}

	public class PackageLevel : CompilerUnitItem
	{
		public PackageLevel(PackageAction action, string[] levels)
		{
			Action = action;
			Levels = levels;
		}

		public PackageAction Action { get; }
		public string[] Levels { get; }
	}
}