using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Terumi.Ast;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Binder
{
	public class ExpressionBinder
	{
		private readonly InfoItem _type;
		private readonly Ast.ThisExpression _thisExpression;
		private readonly TypeInformation _typeInformation;
		private readonly List<(string name, InfoItem type)> _vars = new List<(string name, InfoItem type)>();

		public ExpressionBinder(TypeInformation typeInformation, InfoItem type)
		{
			_type = type;
			_thisExpression = new Ast.ThisExpression(_type);
			_typeInformation = typeInformation;
		}

		public void Bind(InfoItem.Method method)
		{
			foreach (var expression in method.TerumiBacking.Body.Expressions)
			{
				HandleExpression(expression);
			}

			if (method.ReturnType != TypeInformation.Void)
			{
				var isReturn = false;

				// TODO: verify that for each branch there is a way to exit.

				// make sure it returns the proper type
				foreach (var statement in method.Statements)
				{
					if (statement is ReturnStatement returnStatement)
					{
						isReturn = true;

						if (returnStatement.ReturnOn.Type != method.ReturnType)
						{
							throw new Exception($"Returning on a '{returnStatement.ReturnOn.Type.Name}' - suppose to return on a '{method.ReturnType.Name}'.");
						}
					}
				}

				if (!isReturn)
				{
					throw new Exception($"Method {method.Name} expected to return a {method.ReturnType.Name}, but doesn't return anything at all.");
				}
			}

			void HandleExpression(Expression expression)
			{
				switch (expression)
				{
					case ReturnExpression returnExpression:
					{
						method.Statements.Add(new ReturnStatement(TopLevelBind(returnExpression.Expression)));
					}
					break;

					case MethodCall methodCall:
					{
						method.Statements.Add((MethodCallExpression)TopLevelBind(methodCall));
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
								method.Statements.Add(methodCallExpression);
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

						method.Statements.Add(new AssignmentStatement(expr as VariableAssignment));
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
					if (!_type.Code.Parameters.Any(x => x.Name == referenceExpression.ReferenceName))
					{
						// nowhere else to look
						throw new Exception("Unresolved reference '" + referenceExpression.ReferenceName + "'");
					}

					return new ParameterReferenceExpression(_type.Code.Parameters.First(x => x.Name == referenceExpression.ReferenceName));
				}

				case NumericLiteralExpression numericLiteralExpression:
				{
					return new ConstantLiteralExpression<BigInteger>(numericLiteralExpression.LiteralValue);
				}

				case StringLiteralExpression stringLiteralExpression:
				{
					return new ConstantLiteralExpression<string>(stringLiteralExpression.LiteralValue);
				}

				case BooleanLiteralExpression booleanLiteralExpression:
				{
					return new ConstantLiteralExpression<bool>(booleanLiteralExpression.LiteralValue);
				}

				case SyntaxTree.Expressions.ThisExpression _:
				{
					return new Ast.ThisExpression(_type);
				}

				case VariableExpression variableExpression:
				{
					var valueExpr = TopLevelBind(variableExpression.Value);

					var name = variableExpression.Identifier.Identifier;

					if (variableExpression.Type != null)
					{
						if (!_typeInformation.TryGetItem(_type, variableExpression.Type.TypeName.Identifier, out var type))
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
			entity ??= _thisExpression;

			var expressions = ParseMethodCallExpressions(methodCall);

			if (methodCall.IsCompilerMethodCall)
			{
				var call = CompilerEntity.MatchMethod(methodCall.MethodName.Identifier, expressions.Select(x => x.Type));

				return new MethodCallExpression(entity, call.Type.Code, expressions.ToList());
			}

			foreach (var referencedItem in _typeInformation.AllReferenceableTypes(entity.Type))
			{
				if (referencedItem.Name == methodCall.MethodName.Identifier
					&& ParametersMatch(referencedItem.Code.Parameters, expressions, out var parameters))
				{
					return new MethodCallExpression(entity, referencedItem.Code, parameters.AsReadOnly());
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
			ICollection<InfoItem.Method.Parameter> parametersDefinition,
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