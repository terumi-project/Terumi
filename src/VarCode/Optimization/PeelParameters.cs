using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.VarCode.Optimization
{
	/// <summary>
	/// This optimization peels off unnecessary parameters.
	/// 
	/// <code>
	/// do_thing(number a, number b, number c)
	/// {
	///		@println(to_string(a))
	/// }
	/// </code>
	/// 
	/// will have <c>number b</c> and <c>number c</c> "peeled" off, as such:
	/// 
	/// <code>
	/// do_thing(number a)
	/// {
	///		@println(to_string(a))
	/// }
	/// </code>
	/// 
	/// This is particularly useful to reduce the amount of parameters needed
	/// to pass to methods (less to allocate in C code) which do not require
	/// the class they're in, eg. dummy getters:
	/// 
	/// <code>
	/// class Product
	/// {
	///		string id() {
	///			return "cool-product"
	///		}
	/// }
	/// </code>
	/// </summary>
	public class PeelParameters
	{
		public static bool Peel(Method method)
		{
			var used = new List<int>();
			Find(method.Code, used);

			// all parameters are used
			if (used.Count == method.Parameters.Count)
			{
				return false;
			}

			used.Sort();

			// <parameterIndex> -> <new index>
			var map = new Dictionary<int, int>();

			for (int i = 0; i < used.Count; i++)
			{
				map[used[i]] = i;
			}

			Replace(method.Code, map);

			for (int i = used.Count - 1; i >= 0; i--)
			{
				if (!used.Contains(i))
				{
					method.Parameters.RemoveAt(i);
				}
			}

			return true;
		}

		private static void Find(List<Instruction> instructions, List<int> used)
		{
			foreach (var i in instructions)
			{
				switch (i)
				{
					case Instruction.Load.Parameter o:
					{
						if (!used.Contains(o.ParameterNumber))
						{
							used.Add(o.ParameterNumber);
						}
					}
					break;

					case Instruction.If o:
					{
						Find(o.Clause, used);
					}
					break;

					case Instruction.While o:
					{
						Find(o.Clause, used);
					}
					break;
				}
			}
		}

		private static void Replace(List<Instruction> instructions, Dictionary<int, int> map)
		{
			foreach (var i in instructions)
			{
				switch (i)
				{
					case Instruction.Load.Parameter o:
					{
						o.ParameterNumber = map[o.ParameterNumber];
					}
					break;

					case Instruction.If o:
					{
						Replace(o.Clause, map);
					}
					break;

					case Instruction.While o:
					{
						Replace(o.Clause, map);
					}
					break;
				}
			}
		}
	}
}
