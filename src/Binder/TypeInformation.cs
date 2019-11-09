using System;
using System.Collections.Generic;
using System.Linq;

namespace Terumi.Binder
{
	public class TypeInformation
	{
		public static InfoItem Void { get; } = new InfoItem
		{
			IsCompilerDefined = true,
			Name = "void"
		};

		public static InfoItem String { get; } = new InfoItem
		{
			IsCompilerDefined = true,
			Name = "string"
		};

		public static InfoItem Number { get; } = new InfoItem
		{
			IsCompilerDefined = true,
			Name = "number"
		};

		public static InfoItem Boolean { get; } = new InfoItem
		{
			IsCompilerDefined = true,
			Name = "bool"
		};

		public List<IBind> Binds { get; set; } = new List<IBind>();

		public IEnumerable<IBind> AllReferenceableTypes(IBind mainType)
		{
			var namespaces = new List<PackageLevel>(mainType.References);
			namespaces.Add(mainType.Namespace);

			foreach (var item in Binds)
			{
				if (!namespaces.Contains(item.Namespace))
				{
					continue;
				}

				/*
				TODO: figure out why this was put here
				// if the type is itself, skip it
				if (item.Equals(mainType))
				{
					continue;
				}
				*/

				yield return item;
			}

			yield return Void;
			yield return String;
			yield return Number;
			yield return Boolean;
		}

		public bool TryGetItem(IBind mainType, string typeName, out IBind type)
		{
			foreach (var item in AllReferenceableTypes(mainType))
			{
				if (item.Name == typeName)
				{
					type = item;
					return true;
				}
			}

			type = default;
			return false;
		}

		public bool TryGetType(IBind mainType, string typeName, out InfoItem type)
		{
			foreach (var item in AllReferenceableTypes(mainType))
			{
				if (item is InfoItem infoItem
					&& item.Name == typeName)
				{
					type = infoItem;
					return true;
				}
			}

			type = default;
			return false;
		}
	}
}