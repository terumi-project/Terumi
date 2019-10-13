using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Ast.Code;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Workspace.TypePasser
{
	public class ExpressionBinder
	{
		private readonly TypeInformation _typeInformation;
		private readonly InfoItem _type;
		private readonly ThisExpression _thisExpression;

		public ExpressionBinder(TypeInformation typeInformation, InfoItem type)
		{
			_typeInformation = typeInformation;
			_type = type;
			_thisExpression = new ThisExpression(_type);
		}

		public void Bind(InfoItem.Method method)
		{
			foreach (var expression in method.TerumiBacking.Body.Expressions)
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

			foreach(var expression in methodCall.Parameters.Expressions)
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
