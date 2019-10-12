using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Workspace.TypePasser;

namespace Terumi.Ast.Code
{
	public class ThisExpression : ICodeExpression
	{
		public ThisExpression(InfoItem type)
		{
			Type = type;
		}

		public InfoItem Type { get; }
	}
}
