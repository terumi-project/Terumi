using System.Collections.Generic;

namespace Terumi.Binder
{
	public class InfoItem
	{
		/// <summary>
		/// If this is true, <see cref="Namespace"/> is meaningless.
		/// </summary>
		public bool IsCompilerDefined { get; set; }

		public ICollection<ICollection<string>> NamespaceReferences = new List<ICollection<string>>(4);

		public ICollection<string> Namespace { get; set; } = new List<string>(5);

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