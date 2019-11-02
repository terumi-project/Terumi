using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Binder;

namespace Terumi.Ast
{
	public class CompilerEntity : ICodeExpression
	{
		public static ICodeExpression Instance { get; } = new CompilerEntity();

		public InfoItem Type { get; } = new InfoItem
		{
			// TOOD: define compiler stuff whenever lol
			Code = new InfoItem.Method
			{
				Name = "println",
				Parameters = new List<InfoItem.Method.Parameter>
				{
					new InfoItem.Method.Parameter
					{
						Name = "p1",
						Type = TypeInformation.Number
					}
				}
			}
		};
	}
}
