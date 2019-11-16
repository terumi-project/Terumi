using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Terumi.Binder
{
	public interface IBind
	{
		PackageLevel Namespace { get; }

		List<PackageLevel> References { get; }

		string Name { get; }
	}

	public interface IType : IBind
	{
		// TODO: fields, methods, ...
	}

	public interface IMethod : IBind
	{
		public IType ReturnType { get; }

		public List<ParameterBind> Parameters { get; }
	}

	public class MethodBind : IMethod
	{
		public PackageLevel Namespace { get; set; }

		public List<PackageLevel> References { get; set; } = new List<PackageLevel>();

		public string Name { get; set; }

		public IType ReturnType { get; set; }

		public List<ParameterBind> Parameters { get; set; } = new List<ParameterBind>();

		// MethodBind specific
		public SyntaxTree.Method? TerumiBacking { get; set; }

		public List<Ast.CodeStatement> Statements { get; set; } = new List<Ast.CodeStatement>();
	}

	public class ParameterBind
	{
		public IType Type { get; set; }

		public string Name { get; set; }
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

	public class CompilerMethod : IMethod, IEquatable<CompilerMethod>
	{
		public PackageLevel Namespace { get; }

		public List<PackageLevel> References => EmptyList<PackageLevel>.Instance;

		public IType ReturnType { get; set; }

		public string Name { get; set; }

		public List<ParameterBind> Parameters { get; set; }

		public Func<List<string>, string> Generate { get; set; }

		public override bool Equals(object obj) => Equals(obj as CompilerMethod);
		public bool Equals([AllowNull] CompilerMethod other) => other != null && Namespace.Equals(other.Namespace) && EqualityComparer<List<PackageLevel>>.Default.Equals(References, other.References) && EqualityComparer<IType>.Default.Equals(ReturnType, other.ReturnType) && Name == other.Name && EqualityComparer<List<ParameterBind>>.Default.Equals(Parameters, other.Parameters) && EqualityComparer<Func<List<string>, string>>.Default.Equals(Generate, other.Generate);
		public override int GetHashCode() => HashCode.Combine(Namespace, References, ReturnType, Name, Parameters, Generate);

		public static bool operator ==(CompilerMethod left, CompilerMethod right) => EqualityComparer<CompilerMethod>.Default.Equals(left, right);
		public static bool operator !=(CompilerMethod left, CompilerMethod right) => !(left == right);
	}

	public class CompilerType : IType
	{
		public string Name { get; set; }

		public PackageLevel Namespace { get; }

		public List<PackageLevel> References => EmptyList<PackageLevel>.Instance;

		// TODO: fields...
	}
}