﻿using System;

using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class AccessExpressionPattern : INewPattern<AccessExpression>
	{
		public INewPattern<Expression> ExpressionPattern { get; set; }

		public int TryParse(TokenStream stream, ref AccessExpression item)
		{
			if (ExpressionPattern == null) throw new ArgumentException("Set ExpressionPattern in " + nameof(AccessExpressionPattern));
			if (!stream.NextChar('.')) return 0;

			if (!stream.TryParse(ExpressionPattern, out var expression))
			{
				Log.Error($"Expected expression after dot, didn't get one {stream.TopInfo}");
				return 0;
			}

			item = new AccessExpression { Access = expression };
			return stream;
		}
	}
}