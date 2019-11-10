using System;
using System.Collections.Generic;
using System.Linq;
using Terumi.Ast;
using Terumi.Workspace;

namespace Terumi.Binder
{
	public class BinderEnvironment
	{
		public TypeInformation TypeInformation { get; set; } = new TypeInformation();

		private readonly List<ParsedProjectFile> _sourceFiles;

		public BinderEnvironment(List<ParsedProjectFile> sourceFiles)
			=> _sourceFiles = sourceFiles;

		public void PassOverTypeDeclarations()
		{
			// first, do a rough pass over all the type declarations in the source files
			// we want to grab if it's a contract/class, its name, its namespace, and the namespaces it references

			foreach (var file in _sourceFiles)
			{
				foreach (var method in file.Methods)
				{
					var bind = new MethodBind
					{
						Namespace = file.Namespace,
						References = file.Usings,
						Name = method.Identifier,

						// ReturnType <handled below>
						// Parameters <handled below>
						TerumiBacking = method,
						// Statements <handled at a later step>
					};

					bind.ReturnType = GetType(method, bind);
					bind.Parameters = method.Parameters.Select(x => new ParameterBind
					{
						Name = x.Name.Identifier,
						Type = TypeInformation.TryGetType(bind, x.Type.TypeName, out var paramType)
									? paramType
									: throw new Exception($"Couldn't find paramter type '{x.Type.TypeName}'")
					}).ToList();

					// check if anything similar already exists
					if (TypeInformation.Binds.Any(x => x.Name == bind.Name && x.Namespace.SequenceEqual(bind.Namespace)))
					{
						throw new Exception($"Duplicate declaration: {bind.Namespace} '{bind.Name}'");
					}

					TypeInformation.Binds.Add(bind);
				}
			}

			// now, we ensure that every type we parsed can't reference 2+ of the same named type
			// eg. if there was `Parser` in dep_a and `Parser` in dep_b, we wouldn't want anything to be using dep_a and using dep_b

			foreach (var bind in TypeInformation.Binds)
			{
				var namespaces = new List<PackageLevel>(bind.References)
				{
					bind.Namespace
				};

				if (TypeInformation.Binds.Where(x => namespaces.Contains(x.Namespace))
					.Count(x => x.Name == bind.Name) > 1)
				{
					// so, in the main namespace OR in the namespaces references
					// there are more than 1 definitions of the same name from within the references namespaces or the same namespace
					throw new Exception($"Duplicate definition - the same name exists either within the namespace, or referenced namespaces: '{bind.Name}'.");
				}
			}

			// we're good :D
		}

		private IType GetType(SyntaxTree.Method method, MethodBind bind)
		{
			if (method.Type == null)
			{
				return CompilerDefined.Void;
			}
			else if (TypeInformation.TryGetType(bind, method.Type, out var returnType))
			{
				return returnType;
			}

			throw new Exception($"Couldn't find method return type '{method.Type}'");
		}

		// now that we've passed over both the type declarations, method declarations,
		// we can start to parse the method bodies themselves.
		public void PassOverMethodBodies()
		{
			foreach (var bind in TypeInformation.Binds)
			{
				if (bind is MethodBind methodBind)
				{
					var examiner = new ExpressionBinder(TypeInformation);

					examiner.Bind(methodBind);
				}
			}
		}
	}
}