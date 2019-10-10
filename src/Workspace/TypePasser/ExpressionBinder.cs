using System;
using System.Collections.Generic;
using System.Text;
using Terumi.SyntaxTree.Expressions;

namespace Terumi.Workspace.TypePasser
{
	public class ExpressionBinder
	{
		private readonly TypeInformation _typeInformation;
		private readonly InfoItem _type;

		public ExpressionBinder(TypeInformation typeInformation, InfoItem type)
		{
			_typeInformation = typeInformation;
			_type = type;
		}

		public void Bind(InfoItem.Method method)
		{
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

				}
				break;

				default:
				{
					throw new Exception("Unsupported expression value type " + expression.GetType().FullName);
				}
			}
		}
	}
}
