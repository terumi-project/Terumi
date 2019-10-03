namespace Terumi.Ast
{
	public class Field : Member
	{
		public Field(string name, ICompilationType type)
		{
			Name = name;
			Type = type;
		}

		public string Name { get; }
		public bool IsPrivate => Name.StartsWith('_');

		public ICompilationType Type { get; }
	}
}