using System;
using System.Collections.Generic;
using System.Linq;

using Terumi.Ast;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Binder
{
	public class ExpressionBinder
	{
		private readonly TypeInformation _typeInformation;
		private readonly List<(string name, UserType type)> _vars = new List<(string name, UserType type)>();
		private readonly MethodBind _method;

		public ExpressionBinder(TypeInformation typeInformation, MethodBind method)
		{
			_typeInformation = typeInformation;
			_method = method;
		}

		public void Bind()
		{
			foreach (var expression in _method.TerumiBacking.Body.Expressions)
			{
				HandleExpression(expression);
			}

			if (_method.ReturnType != TypeInformation.Void)
			{
				var isReturn = false;

				// TODO: verify that for each branch there is a way to exit.

				// make sure it returns the proper type
				foreach (var statement in _method.Statements)
				{
					if (statement is ReturnStatement returnStatement)
					{
						isReturn = true;

						if (returnStatement.ReturnOn.Type != _method.ReturnType)
						{
							throw new Exception($"Returning on a '{returnStatement.ReturnOn.Type.Name}' - suppose to return on a '{_method.ReturnType.Name}'.");
						}
					}
				}

				if (!isReturn)
				{
					throw new Exception($"Method {_method.Name} expected to return a {_method.ReturnType.Name}, but doesn't return anything at all.");
				}
			}

			void HandleExpression(Expression expression)
			{
				switch (expression)
				{
					case ReturnExpression returnExpression:
					{
						_method.Statements.Add(new ReturnStatement(TopLevelBind(returnExpression.Expression)));
					}
					break;

					case MethodCall methodCall:
					{
						_method.Statements.Add((MethodCallExpression)TopLevelBind(methodCall));
					}
					break;

					case AccessExpression accessExpression:
					{
						var predecessor = TopLevelBind(accessExpression.Predecessor);

						var action = TopLevelBind(accessExpression.Access, predecessor);

						switch (action)
						{
							case MethodCallExpression methodCallExpression:
							{
								_method.Statements.Add(methodCallExpression);
							}
							break;

							default:
							{
								throw new Exception("Invalid access expression in code body: " + action.GetType().FullName);
							}
						}
					}
					break;

					case VariableExpression variableExpression:
					{
						var expr = TopLevelBind(variableExpression);

						_method.Statements.Add(new AssignmentStatement(expr as VariableAssignment));
					}
					break;

					default:
					{
						throw new Exception("Invalid expression in code body: " + expression.GetType().FullName);
					}
				}
			}
		}

		public ICodeExpression TopLevelBind(Expression expression, ICodeExpression entityReference = null)
		{
			// primarily used for literals
			if (expression is ICodeExpression codeExpression)
			{
				return codeExpression;
			}

			switch (expression)
			{
				case MethodCall methodCall:
				{
					return HandleMethodCall(methodCall, entityReference);
				}

				case AccessExpression accessExpression:
				{
					var predecessor = TopLevelBind(accessExpression.Predecessor);

					return TopLevelBind(accessExpression.Access, predecessor);
				}

				case ReferenceExpression referenceExpression:
				{
					// the only named things we should be able to reference are variables and parameters
					var varName = _vars.Find(x => x.name == referenceExpression.ReferenceName);

					if (varName != default)
					{
						// TODOdoododododoojiiiiiiiiiiiiiiiiijoiiiiiiiiiiiiiijoooooooooooojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojojo
						return new VariableReferenceExpression(varName.name, varName.type);
					}

					// now check for parameters
					if (!_method.Parameters.Any(x => x.Name == referenceExpression.ReferenceName))
					{
						// nowhere else to look
						throw new Exception("Unresolved reference '" + referenceExpression.ReferenceName + "'");
					}

					return new ParameterReferenceExpression(_method.Parameters.First(x => x.Name == referenceExpression.ReferenceName));
				}

				case SyntaxTree.Expressions.ThisExpression _:
				{
					throw new NotImplementedException();
					// return new Ast.ThisExpression(_type);
				}

				case VariableExpression variableExpression:
				{
					var valueExpr = TopLevelBind(variableExpression.Value);

					var name = variableExpression.Identifier.Identifier;

					if (variableExpression.Type != null)
					{
						if (!_typeInformation.TryGetType(_method, variableExpression.Type.TypeName.Identifier, out var type))
						{
							throw new Exception($"Unable to find variable type '{variableExpression.Type.TypeName.Identifier}' for variable '{name}'");
						}

						if (_vars.Any(x => x.name == name))
						{
							throw new Exception($"Variable '{name}' already defined.");
						}

						_vars.Add((name, type));
					}
					else
					{
						if (!_vars.Any(x => x.name == name))
						{
							_vars.Add((name, valueExpr.Type));
						}
					}

					return new VariableAssignment(name, valueExpr);
				}

				default:
				{
					throw new Exception("Unsupported expression value type " + expression.GetType().FullName);
				}
			}
		}

		private ICodeExpression HandleMethodCall(MethodCall methodCall, ICodeExpression entity = null)
		{
			var expressions = ParseMethodCallExpressions(methodCall);

			if (methodCall.IsCompilerMethodCall)
			{
				var call = CompilerDefined.MatchMethod(methodCall.MethodName.Identifier, expressions.Select(x => x.Type).ToArray());

				return new MethodCallExpression(entity, call, expressions.ToList());
			}

			foreach (var referencedItem in _typeInformation.AllReferenceableTypes(entity.Type).OfType<MethodBind>())
			{
				if (referencedItem.Name == methodCall.MethodName.Identifier
					&& ParametersMatch(referencedItem.Parameters, expressions, out var parameters))
				{
					return new MethodCallExpression(entity, referencedItem, parameters.AsReadOnly());
				}
			}

			throw new InvalidOperationException("Couldn't parse MethodCallExpression");
		}

		private ICollection<ICodeExpression> ParseMethodCallExpressions(MethodCall methodCall)
		{
			var expressions = new List<ICodeExpression>();

			foreach (var expression in methodCall.Parameters.Expressions)
			{
				expressions.Add(TopLevelBind(expression));
			}

			return expressions;
		}

		private bool ParametersMatch
		(
			ICollection<MethodBind.Parameter> parametersDefinition,
			ICollection<ICodeExpression> passedExpressions,
			out List<ICodeExpression> parameters
		)
		{
			if (parametersDefinition.Count != passedExpressions.Count)
			{
				parameters = default;
				return false;
			}

			parameters = new List<ICodeExpression>();

			for (var i = 0; i < passedExpressions.Count; i++)
			{
				var parameter = passedExpressions.ElementAt(i);

				parameters.Add(parameter);

				if (parameter.Type != parametersDefinition.ElementAt(i).Type)
				{
					return false;
				}
			}

			return true;
		}
	}
}