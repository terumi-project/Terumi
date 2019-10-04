using System;
using System.Collections.Generic;

namespace Terumi.Binder
{
	public class Cache<T>
	{
		public Cache(Func<T, T, bool>? cmp = null)
		{
			_cmp = cmp ?? ((a, b) => a.Equals(b));
		}

		private readonly List<T> _items = new List<T>();
		private readonly Func<T, T, bool> _cmp;

		public T GetInstance(T instance)
		{
			foreach (var item in _items)
			{
				if (_cmp(item, instance))
				{
					return item;
				}
			}

			_items.Add(instance);
			return instance;
		}
	}
}