﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Terumi.ShellNeutral
{
	public class Writer
	{
		public List<CodeLine> Code { get; } = new List<CodeLine>();

		public Writer WriteLine(CodeLine line)
		{
			Code.Add(line);
			return this;
		}

		public Writer Set(BigInteger variable, CodeExpression expression)
			=> WriteLine(new CodeLine(variable, expression));

		public Writer CallLabel(BigInteger label)
			=> WriteLine(new CodeLine(label, false, false));

		public Writer CallCompiler(BigInteger compilerFunctionId)
			=> WriteLine(new CodeLine(compilerFunctionId));

		public Writer Goto(BigInteger label)
			=> WriteLine(new CodeLine(label, true, false));

		public Writer Place(BigInteger label)
			=> WriteLine(new CodeLine(label, false, true));

		public Writer Pop()
			=> WriteLine(CodeLine.Pop);
	}
}
