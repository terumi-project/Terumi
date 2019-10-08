using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terumi.Ast;
using Terumi.Ast.Code;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Binder
{
	public class ExpressionBinder
	{
		private readonly MethodDefinition _methodDefinition;
		private readonly SyntaxTree.TypeDefinition _typeDefinition;

		public ExpressionBinder(MethodDefinition methodDefinition, SyntaxTree.TypeDefinition typeDefinition)
		{
			_methodDefinition = methodDefinition;
			_typeDefinition = typeDefinition;
		}

		public CodeStatement Bind(Expression expression)
		{
			switch (expression)
			{
				case ReturnExpression returnExpr:
				{
					return new ReturnStatement(BindExpression(returnExpr.Expression));
				}

				case AccessExpression access:
				{
					var expr = BindExpression(access);
					return default;
				}

				default:
				{
					throw new Exception("Unexpected expression '" + expression.GetType().FullName + "' in method '" + _methodDefinition.ToString());
				}
			}
		}

		public ICodeExpression BindExpression(Expression expression)
		{
			switch (expression)
			{
				case AccessExpression access:
				{
					var primary = BindExpression(access.Predecessor);
					var action = BindExpression(access.Access);

					return new InvocationStatement(primary, action);
				}

				case MethodCall methodCall:
				{
					var methodName = methodCall.MethodName.Identifier;

					var callingMethod = _typeDefinition.Members.OfType<SyntaxTree.Method>().FirstOrDefault(x => x.Identifier.Identifier == methodName);

					if (callingMethod == null)
					{
						throw new Exception("Couldn't find method on self");
					}

					if (methodCall.Parameters.Expressions.Length != callingMethod.Parameters.Parameters.Length)
					{
						var expect = methodCall.Parameters.Expressions.Length;
						var got = callingMethod.Parameters.Parameters.Length;
						throw new Exception($"Expected {expect} parameters, got {got} parameters instead.");
					}

					// TODO: type checking

					var parameters = new List<ICodeExpression>(methodCall.Parameters.Expressions.Length);

					for (var i = 0; i < parameters.Count; i++)
					{
						parameters[methodCall.Parameters.Expressions.Length] = BindExpression(methodCall.Parameters.Expressions[i]);
					}

					// gonna assume it's alright
					return new InvocationStatement(ThisExpression.IInstance, new MethodCallExpression(_methodDefinition, parameters.AsReadOnly()));
				}
				break;

				case ReturnExpression returnExpression:
				{
					throw new Exception("Cannot parse return as used as an expression");
				}

				default:
				{
					throw new Exception("Unexpected expression '" + expression.GetType().FullName + "' in method '" + _methodDefinition.ToString());
				}
			}
		}
	}
}
