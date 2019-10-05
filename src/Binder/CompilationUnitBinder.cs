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
			var promisedItems = new List<PromisedCompilationNode>();

			{
				List<PackageLevel> usings = null;
				PackageLevel ns = null;
				foreach (var item in _compilerUnit.CompilerUnitItems)
				{
					switch(item)
					{
						case PackageLevel packageLevel:
						{
							if (packageLevel.Action == PackageAction.Namespace)
							{
								usings = new List<PackageLevel>();
								ns = packageLevel;
							}
							else
							{
								usings.Add(packageLevel);
							}
						}
						break;

						case TypeDefinition typeDefinition:
						{
							promisedItems.Add(new PromisedCompilationNode(typeDefinition, ns, usings.ToArray()));
						}
						break;
					}
				}
			}

			var i = 0;
			foreach (var item in _compilerUnit.CompilerUnitItems)
			{
				switch(item)
				{
					case PackageLevel packageLevel:
					{
						if (packageLevel.Action == PackageAction.Namespace)
						{
							_nsBinder = new NamespaceBinder(new Namespace(packageLevel.Levels), _nodes, promisedItems);
						}
						else
						{
							_nsBinder.AddUsing(new Namespace(packageLevel.Levels));
						}
					}
					break;

					case TypeDefinition typeDefinition:
					{
						 var node = _nsBinder.Bind(typeDefinition);
						_nodes.Add(node);

						var promised = promisedItems[i];
						promised.RealNode = node;
						promisedItems[i] = promised;
						i++;
					}
					break;
				}
			}

			return new CompilationUnit(_nodes.AsReadOnly());
		}
	}
}