﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.SyntaxTree.Expressions
{
	public class MethodCallParameterGroup
	{
		public MethodCallParameterGroup(Expression[] expressions)
		{
			Expressions = expressions;
		}

		public Expression[] Expressions { get; }
	}
}