using System;
using System.Collections.Generic;
using System.Linq;

using Terumi.Workspace;

namespace Terumi.Binder
{
	public class BinderEnvironment
	{
		public TypeInformation TypeInformation { get; set; } = new TypeInformation();

		private readonly IReadOnlyCollection<ParsedProjectFile> _sourceFiles;

		public BinderEnvironment
		(
			IReadOnlyCollection<ParsedProjectFile> sourceFiles
		)
			=> _sourceFiles = sourceFiles;

		public void PassOverTypeDeclarations()
		{
			// first, do a rough pass over all the type declarations in the source files
			// we want to grab if it's a contract/class, its name, its namespace, and the namespaces it references

			foreach (var file in _sourceFiles)
			{
				foreach (var item in file.TypeDefinitions)
				{
					var infoItem = new InfoItem
					{
						IsCompilerDefined = false, // explicit for readability
						Namespace = file.Namespace,
						Name = item.Identifier,
						NamespaceReferences = file.Usings.Select(x => (ICollection<string>)(x.Levels.ToArray())).ToList(),
						TerumiBacking = item
					};

					if (!TypeInformation.TryGetItem(infoItem, item.Method.Type.Identifier, out var returnType))
					{
						throw new Exception($"Couldn't find method return type '{item.Method.Type.Identifier}'");
					}

					infoItem.Code = new InfoItem.Method
					{
						Name = item.Identifier,
						ReturnType = returnType,
						Parameters = item.Method.Parameters.Parameters.Select(x => new InfoItem.Method.Parameter
						{
							Name = x.Name.Identifier,
							Type = TypeInformation.TryGetItem(infoItem, x.Type.TypeName.Identifier, out var paramType)
									? paramType
									: throw new Exception($"Couldn't find paramter type '{x.Type.TypeName.Identifier}'")
						}).ToList(),
						TerumiBacking = item.Method
					};

					// check if anything similar already exists
					if (TypeInformation.InfoItems.Any(x => x.Name == infoItem.Name && x.Namespace.SequenceEqual(infoItem.Namespace)))
					{
						throw new Exception("Duplicate declaration: " + infoItem.Namespace.Aggregate((a, b) => $"{a}.{b}") + " '" + infoItem.Name + "'");
					}

					TypeInformation.InfoItems.Add(infoItem);
				}
			}

			// now, we ensure that every type we parsed can't reference 2+ of the same named type
			// eg. if there was `Parser` in dep_a and `Parser` in dep_b, we wouldn't want anything to be using dep_a and using dep_b

			foreach (var definition in TypeInformation.InfoItems)
			{
				var namespaces = new List<ICollection<string>>(definition.NamespaceReferences)
				{
					definition.Namespace
				};

				if (TypeInformation.InfoItems.Where(x => namespaces.Contains(x.Namespace, SequenceEqualsEqualityComparer<string>.Instance))
					.Count(x => x.Name == definition.Name) > 1)
				{
					// so, in the main namespace OR in the namespaces references
					// there are more than 1 definitions of the same name from within the references namespaces or the same namespace
					throw new Exception($"Duplicate definition - the same name exists either within the namespace, or referenced namespaces: '{definition.Name}'.");
				}
			}

			// we're good :D
		}

		public void PassOverMembers()
		{
			// so now we want to pass over every field/method
			// we want to consume in the type declarations of them
			// we can also refer to all the types we declared in the previous step
		}

		// now that we've passed over both the type declarations, method declarations,
		// we can start to parse the method bodies themselves.
		public void PassOverMethodBodies()
		{
			foreach (var infoItem in TypeInformation.InfoItems)
			{
				if (infoItem.IsCompilerDefined)
				{
					continue;
				}

				var examiner = new ExpressionBinder(TypeInformation, infoItem);

				examiner.Bind(infoItem.Code);
			}
		}
	}
}