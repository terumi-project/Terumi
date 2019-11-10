using System.Collections.Generic;

namespace Terumi.Binder
{
	public interface IBind
	{
		PackageLevel Namespace { get; set; }

		List<PackageLevel> References { get; set; }

		string Name { get; set; }
	}

	public interface IType
	{
		string Name { get; set; }
	}

	public class MethodBind : IBind
	{
		public PackageLevel Namespace { get; set; }

		public List<PackageLevel> References { get; set; } = new List<PackageLevel>();

		public string Name { get; set; }

		public SyntaxTree.Method TerumiBacking { get; set; }

		// specific to method
		public IType ReturnType { get; set; }

		public List<Parameter> Parameters { get; set; } = new List<Parameter>();
		public List<Ast.CodeStatement> Statements { get; set; } = new List<Ast.CodeStatement>();

		public class Parameter
		{
			public IType Type { get; set; }

			public string Name { get; set; }
		}
	}

	public class UserType : IBind, IType
	{
		/// <summary>
		/// If this is true, <see cref="Namespace"/> is meaningless.
		/// </summary>
		public bool IsCompilerDefined { get; set; }

		public List<PackageLevel> References { get; set; } = new List<PackageLevel>();

		public PackageLevel Namespace { get; set; } = default;

		public string Name { get; set; }
	}

	public class CompilerType : IType
	{
		public string Name { get; set; }
	}
}