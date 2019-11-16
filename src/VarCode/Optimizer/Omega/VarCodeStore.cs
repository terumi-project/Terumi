using System;
using System.Collections.Generic;
using System.Text;

namespace Terumi.VarCode.Optimizer.Omega
{
	public class VarCodeStore
	{
		public List<VarInstruction> EntryInstructions { get; set; } = new List<VarInstruction>();
		public List<(VarCodeId Name, List<VarInstruction> Instructions)> Functions { get; set; } = new List<(VarCodeId Name, List<VarInstruction> Instructions)>();
	}
}
