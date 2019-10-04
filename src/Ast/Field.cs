namespace Terumi.Ast
{
	public class Field : Member
	{
		public Field(string name, bool isReadOnly, ICompilationType type)
		{
			Name = name;
			Type = type;
			IsReadOnly = isReadOnly;
		}

		public string Name { get; }
		public bool IsPrivate => Name.StartsWith('_');

		public ICompilationType Type { get; }
		public bool IsReadOnly { get; }
	}
}