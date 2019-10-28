using System;
using System.Collections.Generic;
using System.Linq;

using Terumi.Workspace;

namespace Terumi.Binder
{
	public class BinderEnvironment
	{
		public TypeInformation TypeInformation { get; set; } = new TypeInformation();

		private readonly IReadOnlyCollection<ParsedSourceFile> _sourceFiles;

		public BinderEnvironment
		(
			IReadOnlyCollection<ParsedSourceFile> sourceFiles
		)
			=> _sourceFiles = sourceFiles;

		public void PassOverTypeDeclarations()
		{
			// first, do a rough pass over all the type declarations in the source files
			// we want to grab if it's a contract/class, its name, its namespace, and the namespaces it references

			foreach (var file in _sourceFiles)
			{
				var topLevelMethods = new List<SyntaxTree.TypeDefinition>();

				foreach (var item in file.TypeDefinitions)
				{
					if (item.Type == SyntaxTree.TypeDefinitionType.Method)
					{
						topLevelMethods.Add(item);
						continue;
					}

					var infoItem = new InfoItem
					{
						IsCompilerDefined = false, // explicit for readability
						Namespace = file.Namespace.Levels,
						Name = item.Identifier,
						NamespaceReferences = file.Usings.Select(x => (ICollection<string>)x.Levels).ToList(),
						TerumiBacking = item,
						IsContract = item.Type == SyntaxTree.TypeDefinitionType.Contract
					};

					// check if anything similar already exists
					if (TypeInformation.InfoItems.Any(x => x.Name == infoItem.Name && x.Namespace.SequenceEqual(infoItem.Namespace)))
					{
						throw new Exception("Duplicate declaration: " + infoItem.Namespace.Aggregate((a, b) => $"{a}.{b}") + " '" + infoItem.Name + "'");
					}

					TypeInformation.InfoItems.Add(infoItem);
				}

				var top = new InfoItem
				{
					IsCompilerDefined = false,
					Namespace = file.Namespace.Levels,

					// TODO: more unique name
					Name = "compiler_top_level_methods" + file.Namespace.Levels.Aggregate((a, b) => $"{a}_{b}"),

					NamespaceReferences = file.Usings.Select(x => (ICollection<string>)x.Levels).ToList(),
					TerumiBacking = null,
					IsContract = false,
				};

				foreach(var method in topLevelMethods)
				{
					top.Methods.Add(new InfoItem.Method
					{
						TerumiBacking = method.Members[0] as SyntaxTree.Method
					});
				}

				TypeInformation.InfoItems.Add(top);
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

			var i = 0;
			foreach (var file in _sourceFiles)
			{
				foreach (var item in file.TypeDefinitions)
				{
					// 4 - amt of compielr items
					// TODO: not hack job
					var infoItem = TypeInformation.InfoItems.Skip(4).ElementAt(i++);
					ParseType(item, infoItem);
				}

				var topLevelMethods = TypeInformation.InfoItems.Skip(4).ElementAt(i++);

				// TODO: ez refactor
				ParseType
				(
					new SyntaxTree.TypeDefinition
					(
						null,
						SyntaxTree.TypeDefinitionType.Class,
						topLevelMethods.Methods.Select(x => x.TerumiBacking).ToArray()
					),
					topLevelMethods
				);
			}
		}

		private void ParseType(SyntaxTree.TypeDefinition item, InfoItem infoItem)
		{
			foreach (var member in item.Members)
			{
				switch (member)
				{
					case SyntaxTree.Method method:
					{
						if (!TypeInformation.TryGetItem(infoItem, method.Type.Identifier, out var returnType))
						{
							throw new Exception($"Couldn't find method return type '{method.Type.Identifier}'");
						}

						infoItem.Methods.Add(new InfoItem.Method
						{
							Name = method.Identifier.Identifier,
							ReturnType = returnType,
							Parameters = method.Parameters.Parameters.Select(x => new InfoItem.Method.Parameter
							{
								Name = x.Name.Identifier,
								Type = TypeInformation.TryGetItem(infoItem, x.Type.TypeName.Identifier, out var paramType)
									? paramType
									: throw new Exception($"Couldn't find paramter type '{x.Type.TypeName.Identifier}'")
							}).ToList(),
							TerumiBacking = method
						});
					}
					break;

					case SyntaxTree.Field field:
					{
						if (!TypeInformation.TryGetItem(infoItem, field.Type.Identifier, out var fieldType))
						{
							throw new Exception($"Couldn't find field type '{field.Type.Identifier}'");
						}

						infoItem.Fields.Add(new InfoItem.Field
						{
							Name = field.Name.Identifier,
							Type = fieldType,
							TerumiBacking = field
						});
					}
					break;
				}
			}
		}

		// now that we've passed over both the type declarations, method declarations,
		// we can start to parse the method bodies themselves.
		public void PassOverMethodBodies()
		{
			foreach (var infoItem in TypeInformation.InfoItems)
			{
				var examiner = new ExpressionBinder(infoItem);

				foreach (var method in infoItem.Methods)
				{
					examiner.Bind(method);
				}
			}
		}
	}
}