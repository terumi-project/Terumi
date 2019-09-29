using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.Ast
{
	public class TerumiTypeBody
	{
		public TerumiTypeBody(TerumiMember[] members)
		{
			Members = members;
		}

		public TerumiMember[] Members { get; }
	}
}
