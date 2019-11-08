﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.SyntaxTree.Expressions
{
	public class ReferenceExpression : Expression
	{
		public ReferenceExpression(string referenceName)
		{
			ReferenceName = referenceName;
		}

		public string ReferenceName { get; }
	}
}