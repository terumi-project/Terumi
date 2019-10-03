﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.SyntaxTree.Expressions
{
	public class ReturnExpression : Expression
	{
		public ReturnExpression(Expression expression)
		{
			Expression = expression;
		}

		public Expression Expression { get; }
	}
}