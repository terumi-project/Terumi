using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Ast;
using Terumi.SyntaxTree;

namespace Terumi.Binder
{
	public class NamespaceBinder
	{
		private readonly List<Namespace> _usings = new List<Namespace>();

		public NamespaceBinder(Namespace coreNamespace)
		{
			CoreNamespace = coreNamespace;
		}

		public Namespace CoreNamespace { get; }
		public IReadOnlyCollection<Namespace> Usings => _usings.AsReadOnly();

		public void AddUsing(Namespace @using)
		{
			_usings.Add(@using);
		}

		public CompilationNode Bind(TypeDefinition item, IEnumerable<CompilationNode> allNodes)
		{
			// compute everything we can access
			var resources = allNodes
				.Where(x =>
				{
					if (x is Class @class)
					{
						return new[] { CoreNamespace }
							.Concat(Usings)
							.Any(@using => @using.Equals(@class.Namespace));
					}
					else if (x is Contract @contract)
					{
						return new[] { CoreNamespace }
							.Concat(Usings)
							.Any(@using => @using.Equals(@contract.Namespace));
					}
					else
					{
						throw new ArgumentException(">?");
					}
				})
				.ToList();

			// first, deal with fields
			var fields = item.Members
				.OfType<SyntaxTree.Field>()
				.Select(x =>
				{
					var fieldType = GetCompilationType(x.Type.Identifier);
					return new Ast.Field(x.Name.Identifier, x.Readonly, fieldType);
				});

			// if any of the methods have bodies
			var anyMethodHasBody = item.Members
				.OfType<SyntaxTree.Method>()
				.Any(method => method.Body != null);

			if (anyMethodHasBody)
			{
				// dealign with a class
				// TODO: implement
				return default;
			}
			else
			{
				// dealing with a contract
				// none of the methods have bodies so this will be easy

				var methodMaps = item.Members
					.OfType<SyntaxTree.Method>()
					.Select(method =>
					{
						var returnType = GetCompilationType(method.Type.Identifier);
						var parameters = method.Parameters.Parameters.Select(x => GetParameterType(x.Type)).ToList();

						return new MethodDefinition(method.Identifier.Identifier, returnType, parameters.AsReadOnly());
					});

				var memberDefinitions = fields.Cast<MemberDefinition>()
					.Concat(methodMaps)
					.ToList()
					.AsReadOnly();

				return new Contract(item.Identifier, memberDefinitions, CoreNamespace);
			}

			ICompilationType GetParameterType(ParameterType parameterType)
			{
				if (parameterType.HasInnerParameterType)
				{
					var innerType = GetParameterType(parameterType.InnerParameterType);

					if (parameterType.Array)
					{
						innerType = new Ast.CompilationTypes.Array(innerType);
					}

					return innerType;
				}

				return GetCompilationType(parameterType.TypeName.Identifier);
			}

			ICompilationType GetCompilationType(string typeName)
			{
				return new ICompilationType[]
				{
						new Ast.CompilationTypes.Number(),
						new Ast.CompilationTypes.String(),
						new Ast.CompilationTypes.Void(),
						new Ast.CompilationTypes.Boolean(),
				}
					.Concat(resources.Cast<ICompilationType>())

					// find matching type
					.First(type => type.CompilationTypeName == typeName);
			}
		}
	}
}
