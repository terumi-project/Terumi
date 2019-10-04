using System.Collections.Generic;
using Terumi.Ast;
using Terumi.SyntaxTree;

namespace Terumi.Binder
{
	public class CompilationUnitBinder
	{
		private readonly CompilerUnit _compilerUnit;
		private readonly List<CompilationNode> _nodes = new List<CompilationNode>();
		private NamespaceBinder _nsBinder;

		public CompilationUnitBinder(CompilerUnit compilerUnit)
		{
			_compilerUnit = compilerUnit;
		}

		public CompilationUnit Bind()
		{
			foreach(var item in _compilerUnit.CompilerUnitItems)
			{
				switch(item)
				{
					case PackageLevel packageLevel:
					{
						if (packageLevel.Action == PackageAction.Namespace)
						{
							_nsBinder = new NamespaceBinder(new Namespace(packageLevel.Levels));
						}
						else
						{
							_nsBinder.AddUsing(new Namespace(packageLevel.Levels));
						}
					}
					break;

					case TypeDefinition typeDefinition:
					{
						 var node = _nsBinder.Bind(typeDefinition, _nodes);
						_nodes.Add(node);
					}
					break;
				}
			}

			return new CompilationUnit(_nodes.AsReadOnly());
		}
	}
}