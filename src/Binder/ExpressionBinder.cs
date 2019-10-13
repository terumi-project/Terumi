﻿using System;
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

		public ExpressionBinder(InfoItem type)
		{
			_type = type;
			_thisExpression = new Ast.ThisExpression(_type);
		}

		public void Bind(InfoItem.Method method)
		{
			if (_type.IsContract)
			{
				return;
			}

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

				case NumericLiteralExpression numericLiteralExpression:
				{
					return new ConstantLiteralExpression<BigInteger>(numericLiteralExpression.LiteralValue);
				}

				case SyntaxTree.Expressions.ThisExpression _:
				{
					return new Ast.ThisExpression(_type);
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

			foreach (var referencedItem in _type.Methods)
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