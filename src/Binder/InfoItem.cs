using System.Collections.Generic;

namespace Terumi.Binder
{
	public interface IBind
	{
		PackageLevel Namespace { get; set; }

		List<PackageLevel> References { get; set; }

		string Name { get; set; }
	}

	public class MethodBind : IBind
	{
		public PackageLevel Namespace { get; set; }

		public List<PackageLevel> References { get; set; } = new List<PackageLevel>();

		public string Name { get; set; }

		public SyntaxTree.Method TerumiBacking { get; set; }

		// specific to method
		public InfoItem ReturnType { get; set; }
		public List<InfoItem.Method.Parameter> Parameters { get; set; } = new List<InfoItem.Method.Parameter>();
		public List<Ast.CodeStatement> Statements { get; set; } = new List<Ast.CodeStatement>();
	}

	public class InfoItem : IBind
	{
		/// <summary>
		/// If this is true, <see cref="Namespace"/> is meaningless.
		/// </summary>
		public bool IsCompilerDefined { get; set; }

		public List<PackageLevel> References { get; set; } = new List<PackageLevel>(4);

		public PackageLevel Namespace { get; set; } = default;

		public string Name { get; set; }

		public Method Code { get; set; }

		public SyntaxTree.TypeDefinition TerumiBacking { get; set; }

		public class Method
		{
			public string Name { get; set; }

			public InfoItem ReturnType { get; set; }

			public ICollection<Parameter> Parameters { get; set; }

			public ICollection<Ast.CodeStatement> Statements { get; set; } = new List<Ast.CodeStatement>();

			public SyntaxTree.Method TerumiBacking { get; set; }

			public class Parameter
			{
				public InfoItem Type { get; set; }

				public string Name { get; set; }

				public SyntaxTree.Parameter TerumiBacking { get; set; }
			}
		}
	}
}