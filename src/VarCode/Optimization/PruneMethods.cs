using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terumi.VarCode.Optimization
{
	public class PruneMethods
	{
		private IEnumerable<Task> SearchEntryPoints(List<Method> methods)
		{
			for (int i = 0; i < methods.Count; i++)
			{
				var method = methods[i];

				if (method.IsEntryPoint)
				{
					yield return SearchTree(i).AsTask();
				}
			}
		}

		public static async ValueTask<List<Method>> UsedMethods(List<Method> input)
		{
			var pruner = new PruneMethods(input.ToArray());

			await Task.WhenAll(pruner.SearchEntryPoints(input))
				.ConfigureAwait(false);

			var result = new List<Method>(input.Count);
			result.AddRange(pruner.AllReferenceable());
			return result;
		}

		private readonly Method[] _methods;
		private bool[] _referenced;

		private PruneMethods(Method[] methods)
		{
			_methods = methods;
			_referenced = new bool[methods.Length];
		}

		public IEnumerable<Method> AllReferenceable()
		{
			for (int i = 0; i < _referenced.Length; i++)
			{
				if (_referenced[i])
				{
					yield return _methods[i];
				}
			}
		}

		public async ValueTask SearchTree(int methodIndex)
		{
			if (_referenced[methodIndex])
			{
				return;
			}

			_referenced[methodIndex] = true;

			var method = _methods[methodIndex];
			await SearchCodeBody(method.Code).ConfigureAwait(false);
		}

		private async ValueTask SearchCodeBody(List<Instruction> instructions)
		{
			await Task.WhenAll(TasksSearchCodeBody(instructions));
		}

		private IEnumerable<Task> TasksSearchCodeBody(List<Instruction> instructions)
		{
			foreach (var i in instructions)
			{
				switch (i)
				{
					case Instruction.If o:
					{
						foreach (var task in TasksSearchCodeBody(o.Clause))
						{
							yield return task;
						}
					}
					break;

					case Instruction.While o:
					{
						foreach (var task in TasksSearchCodeBody(o.Clause))
						{
							yield return task;
						}
					}
					break;

					case Instruction.Call o:
					{
						var methodIndex = Array.IndexOf(_methods, o.Method);

						if (methodIndex == -1)
						{
							throw new InvalidOperationException();
						}

						if (!_referenced[methodIndex])
						{
							yield return SearchTree(methodIndex).AsTask();
						}
					}
					break;
				}
			}
		}
	}
}
