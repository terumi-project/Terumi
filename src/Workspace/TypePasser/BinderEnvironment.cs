using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Terumi.Workspace.TypePasser
{
	public class BinderEnvironment
	{
		public TypeInformation TypeInformation { get; set; } = new TypeInformation();

		private readonly IReadOnlyCollection<ParsedSourceFile> _sourceFiles;

		public BinderEnvironment
		(
			IReadOnlyCollection<ParsedSourceFile> sourceFiles
		)
		{
			_sourceFiles = sourceFiles;
		}

		public void PassOverTypeDeclarations()
		{
			foreach (var file in _sourceFiles)
			{
				foreach (var item in file.TypeDefinitions)
				{
					var infoItem = new InfoItem
					{
						IsCompilerDefined = false, // explicit for readability
						Namespace = file.Namespace.Levels,
						Name = item.Identifier,
						NamespaceReferences = file.Usings.Select(x => (ICollection<string>)x.Levels).ToList()
					};

					// check if anything similar already exists
					if (TypeInformation.InfoItems.Any(x => x.Name == infoItem.Name && x.Namespace.SequenceEqual(infoItem.Namespace)))
					{
						throw new Exception("Duplicate declaration: " + infoItem.Namespace.Aggregate((a, b) => $"{a}.{b}") + " '" + infoItem.Name + "'");
					}

					TypeInformation.InfoItems.Add(infoItem);
				}
			}

			// now, we pass over what we've done to ensure that a given type can't reference two or more things that are similar

			foreach(var definition in TypeInformation.InfoItems)
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
			var i = 0;
			foreach (var file in _sourceFiles)
			{
				foreach (var item in file.TypeDefinitions)
				{
					var infoItem = TypeInformation.InfoItems.ElementAt(i++);

					foreach(var member in item.Members)
					{
						switch(member)
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
									}).ToList()
								});
							}
							break;

							case SyntaxTree.Field field:
							{
								if (TypeInformation.TryGetItem(infoItem, field.Type.Identifier, out var fieldType))
								{
									throw new Exception($"Couldn't find field type '{field.Type.Identifier}'");
								}

								infoItem.Fields.Add(new InfoItem.Field
								{
									Name = field.Name.Identifier,
									Type = fieldType
								});
							}
							break;
						}
					}
				}
			}
		}
	}
}
