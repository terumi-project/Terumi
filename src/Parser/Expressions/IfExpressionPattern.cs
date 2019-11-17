using System;
using Terumi.SyntaxTree;
using Terumi.SyntaxTree.Expressions;
using Terumi.Tokens;

namespace Terumi.Parser.Expressions
{
	public class IfExpressionPattern : IPattern<IfExpression>
	{
		public IPattern<CodeBody> CodeBodyPattern { get; set; }
		public IPattern<Expression> ExpressionPattern { get; set; }

		public int TryParse(TokenStream stream, ref IfExpression item)
		{
			if (CodeBodyPattern == null) throw new Exception($"CodeBodyPattern set to null");
			if (ExpressionPattern == null) throw new Exception($"IfExpressionPattern set to null");
			if (!stream.NextKeyword(Keyword.If)) return 0;

			if (!stream.TryParse(ExpressionPattern, out var comparison))
			{
				Log.Error($"Couldn't parse an expression for if statement {stream.TopInfo}");
				return 0;
			}

			if (!stream.TryParse(CodeBodyPattern, out var trueBody))
			{
				Log.Error($"Couldn't parse a body for if statement {stream.TopInfo}");
				return 0;
			}

			CodeBody? @false = default;

			if (stream.NextKeyword(Keyword.Else))
			{
				if (!stream.TryParse(CodeBodyPattern, out var falseBody))
				{
					Log.Error($"Couldn't parse the else body for if statement {stream.TopInfo}");
					return 0;
				}

				@false = falseBody;
			}

			item = new IfExpression(comparison, trueBody, @false);
			return stream;
		}
	}
}
