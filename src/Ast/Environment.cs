using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terumi.Ast
{
	/// <summary>
	/// An environment encompassess every dependency's source files
	/// and the actual program's source files, organized by namespace.
	/// </summary>
	public class Environment
	{
		// no ConcurrentDictionary because that's a bit more LOC then a lock
		// me lazy ;)

		public Environment()
		{
			_code = new Dictionary<PackageLevel, List<UsingDescriptor<CompilerUnitItem>>>();
		}

		private readonly object _lock = new object();
		private readonly Dictionary<PackageLevel, List<UsingDescriptor<CompilerUnitItem>>> _code;

		public IReadOnlyDictionary<PackageLevel, IReadOnlyList<UsingDescriptor<CompilerUnitItem>>> Code
			=> (IReadOnlyDictionary<PackageLevel, IReadOnlyList<UsingDescriptor<CompilerUnitItem>>>)_code;

		public void Put(PackageLevel @namespace, IEnumerable<PackageLevel> usings, CompilerUnitItem item)
		{
			if (item is PackageLevel)
			{
				throw new ArgumentException("Cannot put a PackageLevel as an environment item.");
			}

			if (@namespace.Action != PackageAction.Namespace)
			{
				throw new ArgumentException("Must put a Namespace PackageAction as the namespace parameter when putting.");
			}

			if (usings.Any(@using => @using.Action != PackageAction.Using))
			{
				throw new ArgumentException("One of the usings specified is not a Using PackageAction.");
			}

			var usingsArray = usings.ToArray();
			var descriptor = new UsingDescriptor<CompilerUnitItem>(usingsArray, item);

			lock (_lock)
			{
				if (_code.TryGetValue(@namespace, out var items))
				{
					items.Add(descriptor);
				}
				else
				{
					var list = new List<UsingDescriptor<CompilerUnitItem>>();
					list.Add(descriptor);

					_code[@namespace] = list;
				}
			}
		}
	}

	public class UsingDescriptor<T>
	{
		public UsingDescriptor(PackageLevel[] usings, T item)
		{
			Item = item;
			Usings = usings;
		}

		public PackageLevel[] Usings { get; }

		public T Item { get; }
	}
}
