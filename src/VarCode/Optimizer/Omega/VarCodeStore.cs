using System;
using System.Collections.Generic;
using System.Text;
using Terumi.Binder;

namespace Terumi.VarCode.Optimizer.Omega
{
	public class Structure
	{
		public Structure(VarCodeId name, List<VarInstruction> instructions, int parameterCount)
		{
			Name = name;
			Instructions = instructions;
			ParameterCount = parameterCount;
		}

		public VarCodeId Name { get; }
		public List<VarInstruction> Instructions { get; }
		public int ParameterCount { get; }
	}

	public class VarCodeStore
	{
		public List<VarInstruction> Instructions { get; set; } = new List<VarInstruction>();
		public List<Structure> Functions { get; set; } = new List<Structure>();
		public Dictionary<VarCodeId, CompilerMethod> CompilerMethods { get; set; } = new Dictionary<VarCodeId, CompilerMethod>();
	}
}
