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
			var statements = new List<ICodeExpression>();

			foreach(var expression in method.TerumiBacking.Body.Expressions)
			{
				switch(expression)
				{
					case ReturnExpression returnExpression:
					{

					}
					break;

					case MethodCall methodCall:
					{
						statements.Add(TopLevelBind(methodCall));
					}
					break;

					default:
					{
						throw new Exception("Invalid expression in code body: " + expression.GetType().FullName);
					}
				}
			}
		}

		public Ast.Code.ICodeExpression TopLevelBind(Expression expression)
		{
			switch(expression)
			{
				case MethodCall methodCall:
				{
					// if it's a lonely method call, we must assume it's referring to this
					return HandleThisMethodCall(expression, methodCall);
				}
				break;

				default:
				{
					throw new Exception("Unsupported expression value type " + expression.GetType().FullName);
				}
			}
		}

		private ICodeExpression HandleThisMethodCall(Expression expression, MethodCall methodCall)
		{
			foreach (var referencedItem in _type.Methods)
			{
				if (referencedItem.Name == methodCall.MethodName.Identifier
					&& ParametersMatch(referencedItem.Parameters, methodCall.Parameters.Expressions, out var parameters))
				{
					return new MethodCallExpression(_thisExpression, referencedItem, parameters.AsReadOnly());
				}
			}

			throw new InvalidOperationException("Couldn't parse MethodCallExpression");

			bool ParametersMatch
			(
				ICollection<InfoItem.Method.Parameter> parametersDefinition,
				Expression[] passedExpressions,
				out List<ICodeExpression> parameters
			)
			{
				if (parametersDefinition.Count != passedExpressions.Length)
				{
					parameters = default;
					return false;
				}

				parameters = new List<ICodeExpression>();

				for (var i = 0; i < passedExpressions.Length; i++)
				{
					var passedExpression = passedExpressions[i];
					var parameter = TopLevelBind(passedExpression);

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
}
